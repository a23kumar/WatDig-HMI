using System;
using System.Collections.Generic;
using System.IO;
using ModbusHMI.Services;
using Xunit;

namespace ModbusHMI.Tests;

public sealed class MotorSpeedMonitorTests
{
    [Theory]
    [InlineData(0, "0 RPM")]
    [InlineData(1500, "1500 RPM")]
    [InlineData(65535, "65535 RPM")]
    public void StartReadsExactlyOneRegisterAndDisplaysUnsignedSpeed(int speed, string expected)
    {
        var connection = new FakeModbusConnection { Registers = [(ushort)speed, 999] };
        using var monitor = new MotorSpeedMonitor(connection);
        var displays = new List<string>();

        monitor.Start(displays.Add);

        Assert.Equal(new[] { "open", "read" }, connection.Calls);
        Assert.Equal(((byte)1, (ushort)40001, (ushort)1), Assert.Single(connection.Requests));
        Assert.Equal(expected, Assert.Single(displays));
        Assert.True(connection.IsOpen);
    }

    [Fact]
    public void FailedOpenLogsOriginalPrefixAndDoesNotReadOrChangeDisplay()
    {
        var connection = new FakeModbusConnection { OpenError = new IOException("Unavailable") };
        var errors = new List<string>();
        using var monitor = new MotorSpeedMonitor(connection, errors.Add);
        var displays = new List<string>();

        monitor.Start(displays.Add);
        monitor.Stop(displays.Add);

        Assert.Equal("Error: Unavailable", Assert.Single(errors));
        Assert.Equal(new[] { "open" }, connection.Calls);
        Assert.Empty(connection.Requests);
        Assert.Empty(displays);
    }

    [Fact]
    public void FailedReadLeavesDisplayAndConnectionAvailableForStop()
    {
        var connection = new FakeModbusConnection { ReadError = new TimeoutException("Timed out") };
        var errors = new List<string>();
        using var monitor = new MotorSpeedMonitor(connection, errors.Add);
        var displays = new List<string>();

        monitor.Start(displays.Add);

        Assert.Equal("Error reading motor speed: Timed out", Assert.Single(errors));
        Assert.Empty(displays);
        Assert.True(connection.IsOpen);
        monitor.Stop(displays.Add);
        Assert.Equal("Stopped", Assert.Single(displays));
        Assert.False(connection.IsOpen);
    }

    [Fact]
    public void EmptyRegisterResponseIsLoggedWithoutChangingDisplay()
    {
        var connection = new FakeModbusConnection { Registers = [] };
        var errors = new List<string>();
        using var monitor = new MotorSpeedMonitor(connection, errors.Add);
        var displays = new List<string>();

        monitor.Start(displays.Add);

        Assert.StartsWith("Error reading motor speed: ", Assert.Single(errors));
        Assert.Empty(displays);
    }

    [Fact]
    public void StopBeforeStartDoesNothing()
    {
        var connection = new FakeModbusConnection();
        using var monitor = new MotorSpeedMonitor(connection);
        var displays = new List<string>();

        monitor.Stop(displays.Add);

        Assert.Empty(displays);
        Assert.Empty(connection.Calls);
    }

    [Fact]
    public void StopClosesBeforeUpdatingAndRepeatedStopDoesNothing()
    {
        var connection = new FakeModbusConnection();
        using var monitor = new MotorSpeedMonitor(connection);
        monitor.Start(_ => { });
        var displays = new List<string>();

        monitor.Stop(text =>
        {
            Assert.False(connection.IsOpen);
            displays.Add(text);
        });
        monitor.Stop(displays.Add);

        Assert.Equal("Stopped", Assert.Single(displays));
        Assert.Equal(new[] { "open", "read", "close" }, connection.Calls);
    }

    [Fact]
    public void StartAfterStopReadsAgain()
    {
        var connection = new FakeModbusConnection();
        using var monitor = new MotorSpeedMonitor(connection);
        var displays = new List<string>();

        monitor.Start(displays.Add);
        monitor.Stop(displays.Add);
        connection.Registers = [2500];
        monitor.Start(displays.Add);

        Assert.Equal(new[] { "1500 RPM", "Stopped", "2500 RPM" }, displays);
        Assert.Equal(2, connection.Requests.Count);
    }

    [Fact]
    public void RepeatedStartOpenFailureDoesNotReadAgainOrReplaceDisplay()
    {
        var connection = new FakeModbusConnection();
        var errors = new List<string>();
        using var monitor = new MotorSpeedMonitor(connection, errors.Add);
        var displays = new List<string>();
        monitor.Start(displays.Add);

        monitor.Start(displays.Add);

        Assert.Single(connection.Requests);
        Assert.Equal("1500 RPM", Assert.Single(displays));
        Assert.Equal("Error: Port is already open.", Assert.Single(errors));
    }

    [Fact]
    public void FailedClosePropagatesWithoutDisplayingStopped()
    {
        var connection = new FakeModbusConnection { CloseError = new IOException("Close failed") };
        using var monitor = new MotorSpeedMonitor(connection);
        monitor.Start(_ => { });
        var displays = new List<string>();

        Assert.Throws<IOException>(() => monitor.Stop(displays.Add));

        Assert.Empty(displays);
    }

    [Fact]
    public void DisposeReleasesTheConnection()
    {
        var connection = new FakeModbusConnection();
        var monitor = new MotorSpeedMonitor(connection);
        monitor.Start(_ => { });

        monitor.Dispose();

        Assert.True(connection.IsDisposed);
        Assert.False(connection.IsOpen);
    }
}
