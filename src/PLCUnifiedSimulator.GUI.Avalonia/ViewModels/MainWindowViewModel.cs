using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Protocols.Omron;
using PLCUnifiedSimulator.Simulators;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Net;
using System;

namespace PLCUnifiedSimulator.GUI.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "PLC Unified Simulator";

    [ObservableProperty]
    private ObservableCollection<string> protocols = new() { "Mitsubishi", "Omron" };

    [ObservableProperty]
    private string selectedProtocol = "Mitsubishi";

    [ObservableProperty]
    private string hostAddress = "127.0.0.1";

    [ObservableProperty]
    private int port = 502;

    [ObservableProperty]
    private bool isRunning = false;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<string> logMessages = new();

    private IPLCProtocol? _protocol;
    private PLCSimulatorBase? _simulator;

    public MainWindowViewModel()
    {
        LogMessages.Add("Application started");
    }

    [RelayCommand]
    private async Task StartSimulator()
    {
        try
        {
            IsRunning = true;
            StatusMessage = "Starting simulator...";

            // Create protocol based on selection
            _protocol = SelectedProtocol switch
            {
                "Mitsubishi" => new MitsubishiMCProtocol(),
                "Omron" => new OmronFINSProtocol(),
                _ => throw new NotSupportedException($"Protocol {SelectedProtocol} is not supported")
            };

            // Create simulator
            _simulator = SelectedProtocol switch
            {
                "Mitsubishi" => new MitsubishiMCSimulator(),
                "Omron" => new OmronFINSSimulator(),
                _ => throw new NotSupportedException($"Simulator for {SelectedProtocol} is not supported")
            };

            // Start simulator
            await _simulator.StartAsync(Port);
            StatusMessage = $"Simulator running on {HostAddress}:{Port}";
            LogMessages.Add($"Simulator started: {SelectedProtocol} protocol on {HostAddress}:{Port}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            LogMessages.Add($"Error starting simulator: {ex.Message}");
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task StopSimulator()
    {
        try
        {
            if (_simulator != null)
            {
                await _simulator.StopAsync();
                StatusMessage = "Simulator stopped";
                LogMessages.Add("Simulator stopped");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error stopping simulator: {ex.Message}";
            LogMessages.Add($"Error stopping simulator: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
        LogMessages.Add("Log cleared");
    }
}