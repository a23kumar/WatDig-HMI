using Avalonia.Controls;
using System;
using System.IO.Ports;
using NModbus;
using Avalonia.Controls;

namespace ModbusHMI
{
    public partial class MainWindow : Window
    {
        private SerialPort _serialPort;
        private IModbusSerialMaster _modbusMaster;
        private const string COM_PORT = "COM3";  
        private const int BAUD_RATE = 9600;  

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnStartClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                _serialPort = new SerialPort(COM_PORT)
                {
                    BaudRate = BAUD_RATE,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One
                };
                _serialPort.Open();

                var factory = new ModbusFactory();
                _modbusMaster = factory.CreateRtuMaster(_serialPort);

                ReadMotorSpeed();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ReadMotorSpeed()
        {
            try
            {
                byte slaveId = 1;
                ushort startAddress = 40001;  
                ushort numRegisters = 1;

                ushort[] registers = _modbusMaster.ReadHoldingRegisters(slaveId, startAddress, numRegisters);
                ushort motorSpeed = registers[0];

                MotorSpeedText.Text = $"{motorSpeed} RPM";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading motor speed: {ex.Message}");
            }
        }

        private void OnStopClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                MotorSpeedText.Text = "Stopped";
            }
        }
    }
}

