using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ModbusHMI.Services;

namespace ModbusHMI;

public partial class MainWindow : Window
{
    private readonly MotorSpeedMonitor _monitor;

    public MainWindow() : this(new MotorSpeedMonitor(new SerialModbusConnection()))
    {
    }

    public MainWindow(MotorSpeedMonitor monitor)
    {
        InitializeComponent();
        _monitor = monitor;
    }

    private void OnStartClicked(object? sender, RoutedEventArgs e)
        => _monitor.Start(text => MotorSpeedText.Text = text);

    private void OnStopClicked(object? sender, RoutedEventArgs e)
        => _monitor.Stop(text => MotorSpeedText.Text = text);

    protected override void OnClosed(EventArgs e)
    {
        _monitor.Dispose();
        base.OnClosed(e);
    }
}

