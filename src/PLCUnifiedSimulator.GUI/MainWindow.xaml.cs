using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PLCUnifiedSimulator.Simulators;

namespace PLCUnifiedSimulator.GUI;

/// <summary>
/// Device value display model
/// </summary>
public class DeviceValueModel : INotifyPropertyChanged
{
    private string _deviceType = string.Empty;
    private int _address;
    private string _value = string.Empty;

    public string DeviceType
    {
        get => _deviceType;
        set
        {
            _deviceType = value;
            OnPropertyChanged(nameof(DeviceType));
        }
    }

    public int Address
    {
        get => _address;
        set
        {
            _address = value;
            OnPropertyChanged(nameof(Address));
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged(nameof(Value));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private PLCSimulatorBase? _simulator;
    private readonly ObservableCollection<DeviceValueModel> _deviceValues = new();

    public MainWindow()
    {
        InitializeComponent();
        DeviceDataGrid.ItemsSource = _deviceValues;

        // Initialize with some sample device values
        InitializeSampleDevices();
    }

    private void InitializeSampleDevices()
    {
        _deviceValues.Add(new DeviceValueModel { DeviceType = "D", Address = 100, Value = "0" });
        _deviceValues.Add(new DeviceValueModel { DeviceType = "D", Address = 101, Value = "0" });
        _deviceValues.Add(new DeviceValueModel { DeviceType = "M", Address = 100, Value = "False" });
        _deviceValues.Add(new DeviceValueModel { DeviceType = "M", Address = 101, Value = "False" });
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!int.TryParse(PortTextBox.Text, out int port))
            {
                MessageBox.Show("Invalid port number", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Create simulator based on selection
            var plcType = ((ComboBoxItem)PlcTypeComboBox.SelectedItem).Content.ToString();
            _simulator = plcType switch
            {
                "Mitsubishi MC" => new MitsubishiMCSimulator(),
                "Omron FINS" => new OmronFINSSimulator(),
                _ => throw new InvalidOperationException("Unknown PLC type")
            };

            await _simulator.StartAsync(port);

            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            StatusTextBlock.Text = $"Simulator running on port {port}";
            LogTextBox.AppendText($"[{DateTime.Now}] Simulator started on port {port}\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start simulator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogTextBox.AppendText($"[{DateTime.Now}] Error: {ex.Message}\n");
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_simulator != null)
            {
                await _simulator.StopAsync();
                _simulator = null;
            }

            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            StatusTextBlock.Text = "Simulator stopped";
            LogTextBox.AppendText($"[{DateTime.Now}] Simulator stopped\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to stop simulator: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            LogTextBox.AppendText($"[{DateTime.Now}] Error: {ex.Message}\n");
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Clean up simulator on window close
        if (_simulator != null)
        {
            _ = _simulator.StopAsync();
        }
        base.OnClosing(e);
    }
}