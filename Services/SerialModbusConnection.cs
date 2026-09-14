using System;
using System.IO.Ports;
using NModbus;
using NModbus.Serial;

namespace ModbusHMI.Services;

/// <summary>Owns the serial port and its Modbus RTU master.</summary>
public sealed class SerialModbusConnection : IModbusConnection
{
    private readonly SerialPort _port;
    private IModbusSerialMaster? _master;

    public SerialModbusConnection() : this(new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One))
    {
    }

    /// <summary>Takes ownership of the supplied port, including disposal.</summary>
    public SerialModbusConnection(SerialPort port) => _port = port;

    public bool IsOpen => _port.IsOpen;

    public void Open()
    {
        _port.Open();
        try
        {
            _master = new ModbusFactory().CreateRtuMaster(_port);
        }
        catch
        {
            _port.Close();
            throw;
        }
    }

    public ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfRegisters)
        => (_master ?? throw new InvalidOperationException("The Modbus connection is not open."))
            .ReadHoldingRegisters(slaveId, startAddress, numberOfRegisters);

    public void Close()
    {
        _port.Close();
        _master?.Dispose();
        _master = null;
    }

    public void Dispose()
    {
        _master?.Dispose();
        _master = null;
        _port.Dispose();
    }
}
