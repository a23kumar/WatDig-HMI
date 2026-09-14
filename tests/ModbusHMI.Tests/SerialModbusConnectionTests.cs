using System;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ModbusHMI.Services;
using Xunit;

namespace ModbusHMI.Tests;

public sealed class UnixFactAttribute : FactAttribute
{
    public UnixFactAttribute()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
            Skip = "Pseudo-terminal integration requires macOS or Linux.";
    }
}

public sealed class SerialModbusConnectionTests
{
    [UnixFact]
    public async Task RealSerialAdapterReadsRtuFramesAndCanRestartAfterStop()
    {
        using var terminal = new PseudoTerminal();
        using var connection = new SerialModbusConnection(new SerialPort(terminal.Name, 9600, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        });

        for (var cycle = 0; cycle < 2; cycle++)
        {
            connection.Open();
            Assert.True(connection.IsOpen);
            var response = Task.Run(terminal.RespondToRead);
            try
            {
                Assert.Equal(new ushort[] { 1500 }, connection.ReadHoldingRegisters(1, 40001, 1));
            }
            finally
            {
                await response;
            }
            connection.Close();
            Assert.False(connection.IsOpen);
        }
    }

    [UnixFact]
    public void RepeatedOpenDoesNotLoseTheActivePortAndDisposeReleasesIt()
    {
        using var terminal = new PseudoTerminal();
        using var connection = new SerialModbusConnection(new SerialPort(terminal.Name));
        connection.Open();

        Assert.Throws<InvalidOperationException>(connection.Open);
        Assert.True(connection.IsOpen);
        connection.Dispose();
        Assert.False(connection.IsOpen);

        using var replacement = new SerialPort(terminal.Name);
        replacement.Open();
        Assert.True(replacement.IsOpen);
    }

    private sealed class PseudoTerminal : IDisposable
    {
        private readonly int _master;
        private readonly int _slave;
        public string Name { get; }

        public PseudoTerminal()
        {
            var name = new StringBuilder(256);
            var result = OperatingSystem.IsMacOS()
                ? OpenMac(out _master, out _slave, name, IntPtr.Zero, IntPtr.Zero)
                : OpenLinux(out _master, out _slave, name, IntPtr.Zero, IntPtr.Zero);
            if (result != 0) throw new IOException("Could not create a pseudo-terminal.");
            Name = name.ToString();
        }

        public void RespondToRead()
        {
            var request = new byte[8];
            for (var offset = 0; offset < request.Length;)
            {
                var descriptor = new PollDescriptor { FileDescriptor = _master, Events = 1 };
                if (Poll(ref descriptor, 1, 5000) <= 0)
                    throw new TimeoutException("No Modbus request arrived within five seconds.");
                var buffer = new byte[request.Length - offset];
                var count = (int)Read(_master, buffer, (nuint)buffer.Length);
                if (count <= 0) throw new IOException("Pseudo-terminal read failed.");
                buffer.AsSpan(0, count).CopyTo(request.AsSpan(offset));
                offset += count;
            }

            // Slave 1, function 3, address 40001, count 1, little-endian CRC.
            Assert.Equal(new byte[] { 1, 3, 156, 65, 0, 1, 250, 78 }, request);
            byte[] response = [1, 3, 2, 5, 220, 186, 141];
            Assert.Equal(response.Length, (int)Write(_master, response, (nuint)response.Length));
        }

        public void Dispose()
        {
            Close(_master);
            Close(_slave);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PollDescriptor
        {
            public int FileDescriptor;
            public short Events;
            public short ReturnedEvents;
        }

        [DllImport("libSystem.B.dylib", EntryPoint = "openpty")]
        private static extern int OpenMac(out int master, out int slave, StringBuilder name, IntPtr termios, IntPtr winsize);
        [DllImport("libutil.so.1", EntryPoint = "openpty")]
        private static extern int OpenLinux(out int master, out int slave, StringBuilder name, IntPtr termios, IntPtr winsize);
        [DllImport("libc", EntryPoint = "poll")]
        private static extern int Poll(ref PollDescriptor descriptors, nuint count, int timeout);
        [DllImport("libc", EntryPoint = "read")]
        private static extern nint Read(int descriptor, byte[] buffer, nuint count);
        [DllImport("libc", EntryPoint = "write")]
        private static extern nint Write(int descriptor, byte[] buffer, nuint count);
        [DllImport("libc", EntryPoint = "close")]
        private static extern int Close(int descriptor);
    }
}
