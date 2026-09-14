using System;

namespace ModbusHMI.Services;

/// <summary>The serial lifecycle and register operations used by the monitor.</summary>
public interface IModbusConnection : IDisposable
{
    bool IsOpen { get; }
    void Open();
    ushort[] ReadHoldingRegisters(byte slaveId, ushort startAddress, ushort numberOfRegisters);
    void Close();
}
