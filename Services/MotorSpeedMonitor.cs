using System;

namespace ModbusHMI.Services;

/// <summary>Coordinates the existing one-read-per-Start workflow independently of the UI.</summary>
public sealed class MotorSpeedMonitor : IDisposable
{
    private const byte SlaveId = 1;
    // Preserve the address sent by the original application; do not normalize to zero.
    private const ushort MotorSpeedAddress = 40001;
    private const ushort RegisterCount = 1;
    private readonly IModbusConnection _connection;
    private readonly Action<string> _logError;

    public MotorSpeedMonitor(IModbusConnection connection, Action<string>? logError = null)
    {
        _connection = connection;
        _logError = logError ?? Console.WriteLine;
    }

    public void Start(Action<string> updateDisplay)
    {
        try
        {
            _connection.Open();
        }
        catch (Exception ex)
        {
            _logError($"Error: {ex.Message}");
            return;
        }

        try
        {
            var registers = _connection.ReadHoldingRegisters(SlaveId, MotorSpeedAddress, RegisterCount);
            updateDisplay($"{registers[0]} RPM");
        }
        catch (Exception ex)
        {
            _logError($"Error reading motor speed: {ex.Message}");
        }
    }

    public void Stop(Action<string> updateDisplay)
    {
        if (_connection.IsOpen)
        {
            _connection.Close();
            updateDisplay("Stopped");
        }
    }

    public void Dispose() => _connection.Dispose();
}
