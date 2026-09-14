using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ModbusHMI.Services;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ModbusHMI.Tests.TestAppBuilder))]

namespace ModbusHMI.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class MainWindowTests
{
    [AvaloniaFact]
    public void ButtonsPreserveInitialReadStopAndRestartDisplays()
    {
        var connection = new FakeModbusConnection();
        var window = new MainWindow(new MotorSpeedMonitor(connection));
        window.Show();
        try
        {
            var display = window.FindControl<TextBlock>("MotorSpeedText")!;
            Assert.Equal("Motor Speed Monitor", window.Title);
            Assert.Equal(400, window.Width);
            Assert.Equal(300, window.Height);
            Assert.Equal("0 RPM", display.Text);

            Click(window, "Stop");
            Assert.Equal("0 RPM", display.Text);
            Click(window, "Start");
            Assert.Equal("1500 RPM", display.Text);
            Assert.Single(connection.Requests);
            Click(window, "Stop");
            Assert.Equal("Stopped", display.Text);
            Assert.False(connection.IsOpen);
            connection.Registers = [800];
            Click(window, "Start");
            Assert.Equal("800 RPM", display.Text);
        }
        finally
        {
            window.Close();
        }
        Assert.True(connection.IsDisposed);
    }

    [AvaloniaFact]
    public void ReadFailureKeepsLastDisplayedSpeed()
    {
        var connection = new FakeModbusConnection();
        var errors = new List<string>();
        var window = new MainWindow(new MotorSpeedMonitor(connection, errors.Add));
        window.Show();
        try
        {
            Click(window, "Start");
            // Simulate loss of the connection without a user Stop updating the display.
            connection.Close();
            connection.ReadError = new TimeoutException("Timed out");
            Click(window, "Start");

            Assert.Equal("1500 RPM", window.FindControl<TextBlock>("MotorSpeedText")!.Text);
            Assert.Equal("Error reading motor speed: Timed out", Assert.Single(errors));
        }
        finally
        {
            window.Close();
        }
    }

    private static void Click(Window window, string label)
        => window.GetVisualDescendants().OfType<Button>()
            .Single(button => Equals(button.Content, label))
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
}
