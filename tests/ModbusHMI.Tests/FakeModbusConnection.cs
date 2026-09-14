using System;
using System.Collections.Generic;
using ModbusHMI.Services;

namespace ModbusHMI.Tests;

internal sealed class FakeModbusConnection : IModbusConnection
{
    public bool IsOpen { get; private set; }
    public bool IsDisposed { get; private set; }
    public ushort[] Registers { get; set; } = [1500];
    public Exception? OpenError { get; set; }
    public Exception? ReadError { get; set; }
    public Exception? CloseError { get; set; }
    public List<string> Calls { get; } = [];
    public List<(byte Slave, ushort Address, ushort Count)> Requests { get; } = [];

    public void Open()
    {
        Calls.Add("open");
        if (OpenError is not null) throw OpenError;
        if (IsOpen) throw new InvalidOperationException("Port is already open.");
        IsOpen = true;
    }

    public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfRegisters)
    {
        Calls.Add("read");
        if (!IsOpen) throw new InvalidOperationException("Port is closed.");
        Requests.Add((slaveId, startAddress, numberOfRegisters));
        if (ReadError is not null) throw ReadError;
        return Registers;
    }

    public void Close()
    {
        Calls.Add("close");
        if (CloseError is not null) throw CloseError;
        IsOpen = false;
    }

    public void Dispose()
    {
        Calls.Add("dispose");
        IsOpen = false;
        IsDisposed = true;
    }
}
