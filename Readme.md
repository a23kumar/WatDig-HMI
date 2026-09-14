# WatDig HMI

A .NET 8 / Avalonia desktop application that reads and displays motor speed over Modbus RTU.

## Build and run

Install the .NET 8 SDK, then run from the repository root:

```sh
dotnet restore WatDig-HMI-3.sln
dotnet build WatDig-HMI-3.sln --configuration Release
dotnet run --project ModbusHMI.csproj
```

The application uses **COM3, 9600 baud, no parity, 8 data bits, one stop bit**.
Start opens the port and performs one holding-register read: slave **1**, address
**40001**, count **1**. The first unsigned register is displayed as `<value> RPM`.
The address is passed literally to NModbus, matching the original application;
it is not converted to a zero-based offset. There is no polling or register write.

The initial display is `0 RPM`. Stop closes an open connection and displays
`Stopped`; Stop on a closed connection does nothing. Open/read failures are logged
to the console with the existing error prefixes and leave the display unchanged.
Connections are disposed when the window closes. Starting again while already
connected reports an open error and retains the active connection for Stop.

## Structure

- `MainWindow.axaml` / `MainWindow.axaml.cs`: existing layout, button events, and display updates.
- `Services/MotorSpeedMonitor.cs`: Start/Stop workflow, register request, formatting, and error logging.
- `Services/IModbusConnection.cs`: connection contract used by the workflow and test doubles.
- `Services/SerialModbusConnection.cs`: serial settings, NModbus RTU adapter, and resource ownership.
- `tests/ModbusHMI.Tests`: workflow, headless Avalonia UI, and real serial-adapter tests.

The application still uses NModbus 3.0.81 and Avalonia 11.1.0. The serial adapter
and System.IO.Ports references, window XAML class binding, and solution project
path are included so a fresh checkout builds without the previously committed
machine-specific `obj` files.

## Tests

```sh
dotnet test WatDig-HMI-3.sln --configuration Release
```

Tests cover speed limits and formatting, exact register parameters, open/read/close
errors, repeated Start/Stop, restarting, disposal, and real window button wiring.
On macOS and Linux, two additional tests use pseudo-terminals with the real
System.IO.Ports/NModbus adapter to verify RTU request/response bytes, reconnection,
and port release. Those two tests are skipped on Windows, where the workflow and
headless UI tests still run. GitHub Actions builds and tests on Ubuntu and Windows.

No physical motor controller is required for the tests. Before using the app with
hardware, verify Start reads the expected RPM on COM3, Stop displays `Stopped`,
and Start works again after Stop. Automated tests do not validate the actual
controller, wiring, or its register map.
