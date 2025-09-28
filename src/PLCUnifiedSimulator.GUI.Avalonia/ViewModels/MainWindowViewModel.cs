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
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PLCUnifiedSimulator.GUI.Avalonia.ViewModels;

public class PLCSeriesInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DefaultPort { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public MitsubishiPLCSeries? Series { get; set; }
    public bool IsOmron { get; set; }
}

public class RunningSimulatorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SupportedDevicesCount { get; set; }
    public MitsubishiPLCSeries? Series { get; set; }
    public bool IsOmron { get; set; }
    
    // EndPointプロパティを追加
    public string EndPoint => $"{Protocol}://localhost:{Port}";
}

public class DeviceValueInfo
{
    public string DeviceAddress { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
}

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string title = "PLC Unified Simulator";

    // プロトコル選択
    [ObservableProperty]
    private ObservableCollection<string> protocols = new() { "Mitsubishi", "Omron" };

    [ObservableProperty]
    private string selectedProtocol = "Mitsubishi";

    [ObservableProperty]
    private string hostAddress = "127.0.0.1";

    [ObservableProperty]
    private int port = 5000;

    [ObservableProperty]
    private bool isRunning = false;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private ObservableCollection<string> logMessages = new();

    // 利用可能なPLCシリーズ
    [ObservableProperty]
    private ObservableCollection<PLCSeriesInfo> availableSeries = new();

    [ObservableProperty]
    private PLCSeriesInfo? selectedSeries;

    // 実行中のシミュレータ
    [ObservableProperty]
    private ObservableCollection<RunningSimulatorInfo> runningSimulators = new();

    [ObservableProperty]
    private RunningSimulatorInfo? selectedRunningSimulator;

    // テストデータ設定
    [ObservableProperty]
    private string deviceType = "D";

    [ObservableProperty]
    private int deviceAddress = 100;

    [ObservableProperty]
    private int deviceValue = 1234;

    // デバイス値表示
    [ObservableProperty]
    private ObservableCollection<DeviceValueInfo> deviceValues = new();

    // 通信プロトコル設定
    [ObservableProperty]
    private bool isTcpEnabled = true;

    [ObservableProperty]
    private bool isUdpEnabled = false;

    [ObservableProperty]
    private int udpPort = 5100;

    private readonly Dictionary<MitsubishiPLCSeries, MitsubishiMCSimulator> _runningMitsubishiSimulators = new();
    private OmronFINSSimulator? _runningOmronSimulator;
    // private readonly ILogger? _logger;
    // private readonly IServiceProvider? _serviceProvider;

    public MainWindowViewModel()
    {
        LogMessages.Add("Application started");
        LoadAvailableSeries();
    }

    private void LoadAvailableSeries()
    {
        AvailableSeries.Clear();
        
        // 三菱PLCシリーズの追加
        var mitsubishiSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        foreach (var (series, description, port) in mitsubishiSeries)
        {
            AvailableSeries.Add(new PLCSeriesInfo
            {
                Name = series.ToString(),
                Description = description,
                DefaultPort = port,
                Protocol = "Mitsubishi MC",
                Series = series,
                IsOmron = false
            });
        }

        // オムロンFINSの追加
        var omronInfo = OmronPLCSimulatorFactory.GetSimulatorInfo();
        AvailableSeries.Add(new PLCSeriesInfo
        {
            Name = "FINS",
            Description = omronInfo.Description,
            DefaultPort = omronInfo.DefaultPort,
            Protocol = "Omron FINS",
            Series = null,
            IsOmron = true
        });
    }

    [RelayCommand]
    private async Task StartSpecificSimulator()
    {
        if (SelectedSeries == null)
        {
            StatusMessage = "PLCシリーズを選択してください";
            return;
        }

        try
        {
            StatusMessage = $"{SelectedSeries.Name} シミュレータを開始中...";
            LogMessages.Add($"Starting {SelectedSeries.Name} simulator...");

            if (SelectedSeries.IsOmron)
            {
                await StartOmronSimulatorInternal();
            }
            else if (SelectedSeries.Series.HasValue)
            {
                await StartMitsubishiSimulatorInternal(SelectedSeries.Series.Value);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
            LogMessages.Add($"Error starting {SelectedSeries.Name} simulator: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StartAllSimulators()
    {
        StatusMessage = "全てのシミュレータを開始中...";
        LogMessages.Add("Starting all simulators...");

        var startTasks = new List<Task>();
        
        // 三菱シミュレータ
        var mitsubishiSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        foreach (var (series, _, _) in mitsubishiSeries)
        {
            if (!_runningMitsubishiSimulators.ContainsKey(series))
            {
                startTasks.Add(StartMitsubishiSimulatorInternal(series));
            }
        }

        // オムロンシミュレータ
        if (_runningOmronSimulator == null)
        {
            startTasks.Add(StartOmronSimulatorInternal());
        }

        try
        {
            await Task.WhenAll(startTasks);
            StatusMessage = $"完了: {RunningSimulators.Count} シミュレータが実行中";
            LogMessages.Add($"All simulators started successfully. Running: {RunningSimulators.Count}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"一部のシミュレータ開始に失敗: {ex.Message}";
            LogMessages.Add($"Some simulators failed to start: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopSelectedSimulator()
    {
        if (SelectedRunningSimulator == null)
        {
            StatusMessage = "停止するシミュレータを選択してください";
            return;
        }

        try
        {
            StatusMessage = $"{SelectedRunningSimulator.Name} シミュレータを停止中...";
            LogMessages.Add($"Stopping {SelectedRunningSimulator.Name} simulator...");

            if (SelectedRunningSimulator.IsOmron)
            {
                await StopOmronSimulatorInternal();
            }
            else if (SelectedRunningSimulator.Series.HasValue)
            {
                await StopMitsubishiSimulatorInternal(SelectedRunningSimulator.Series.Value);
            }

            StatusMessage = $"{SelectedRunningSimulator.Name} シミュレータを停止しました";
            LogMessages.Add($"{SelectedRunningSimulator.Name} simulator stopped");
        }
        catch (Exception ex)
        {
            StatusMessage = $"エラー: {ex.Message}";
            LogMessages.Add($"Error stopping {SelectedRunningSimulator.Name} simulator: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task StopAllSimulators()
    {
        StatusMessage = "全てのシミュレータを停止中...";
        LogMessages.Add("Stopping all simulators...");

        var stopTasks = new List<Task>();
        
        foreach (var kvp in _runningMitsubishiSimulators.ToList())
        {
            stopTasks.Add(StopMitsubishiSimulatorInternal(kvp.Key));
        }

        if (_runningOmronSimulator != null)
        {
            stopTasks.Add(StopOmronSimulatorInternal());
        }

        try
        {
            await Task.WhenAll(stopTasks);
            StatusMessage = "全てのシミュレータを停止しました";
            LogMessages.Add("All simulators stopped");
        }
        catch (Exception ex)
        {
            StatusMessage = $"一部のシミュレータ停止に失敗: {ex.Message}";
            LogMessages.Add($"Some simulators failed to stop: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SetTestData()
    {
        if (SelectedRunningSimulator == null)
        {
            StatusMessage = "テストデータを設定するシミュレータを選択してください";
            return;
        }

        try
        {
            var address = new PLCAddress(DeviceType, DeviceAddress, 1);
            var data = BitConverter.GetBytes((ushort)DeviceValue);

            if (SelectedRunningSimulator.IsOmron && _runningOmronSimulator != null)
            {
                _runningOmronSimulator.SetDeviceValue(address, data);
            }
            else if (SelectedRunningSimulator.Series.HasValue &&
                     _runningMitsubishiSimulators.TryGetValue(SelectedRunningSimulator.Series.Value, out var simulator))
            {
                simulator.SetDeviceValue(address, data);
            }

            StatusMessage = $"テストデータ設定完了: {DeviceType}{DeviceAddress} = {DeviceValue}";
            LogMessages.Add($"Test data set: {DeviceType}{DeviceAddress} = {DeviceValue}");
            
            // デバイス値表示を更新
            await RefreshDeviceValues();
        }
        catch (Exception ex)
        {
            StatusMessage = $"テストデータ設定エラー: {ex.Message}";
            LogMessages.Add($"Error setting test data: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshDeviceValues()
    {
        if (SelectedRunningSimulator == null)
        {
            return;
        }

        try
        {
            DeviceValues.Clear();

            if (SelectedRunningSimulator.IsOmron && _runningOmronSimulator != null)
            {
                await RefreshOmronDeviceValues();
            }
            else if (SelectedRunningSimulator.Series.HasValue &&
                     _runningMitsubishiSimulators.TryGetValue(SelectedRunningSimulator.Series.Value, out var simulator))
            {
                await RefreshMitsubishiDeviceValues(simulator);
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error refreshing device values: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogMessages.Clear();
        LogMessages.Add("Log cleared");
    }

    [RelayCommand]
    private async Task SetDefaultTestData()
    {
        if (SelectedRunningSimulator == null)
        {
            StatusMessage = "デフォルトテストデータを設定するシミュレータを選択してください";
            return;
        }

        try
        {
            if (SelectedRunningSimulator.IsOmron && _runningOmronSimulator != null)
            {
                await SetDefaultTestDataForOmron(_runningOmronSimulator);
            }
            else if (SelectedRunningSimulator.Series.HasValue &&
                     _runningMitsubishiSimulators.TryGetValue(SelectedRunningSimulator.Series.Value, out var simulator))
            {
                await SetDefaultTestDataForMitsubishi(simulator, SelectedRunningSimulator.Series.Value);
            }

            StatusMessage = "デフォルトテストデータを設定しました";
            LogMessages.Add("Default test data set successfully");
            
            // デバイス値表示を更新
            await RefreshDeviceValues();
        }
        catch (Exception ex)
        {
            StatusMessage = $"デフォルトテストデータ設定エラー: {ex.Message}";
            LogMessages.Add($"Error setting default test data: {ex.Message}");
        }
    }

    private async Task StartMitsubishiSimulatorInternal(MitsubishiPLCSeries series)
    {
        if (_runningMitsubishiSimulators.ContainsKey(series))
        {
            LogMessages.Add($"{series} simulator is already running");
            return;
        }

        try
        {
            var simulator = new MitsubishiMCSimulator(series, null);
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            
            int port = Port > 0 ? Port : seriesInfo.DefaultPort;
            
            if (IsTcpEnabled && IsUdpEnabled)
            {
                await simulator.StartBothAsync(port, UdpPort);
            }
            else if (IsUdpEnabled)
            {
                await simulator.StartUdpAsync(UdpPort);
                port = UdpPort;
            }
            else
            {
                await simulator.StartAsync(port);
            }
            
            _runningMitsubishiSimulators[series] = simulator;
            
            // 実行中リストに追加
            var runningInfo = new RunningSimulatorInfo
            {
                Name = series.ToString(),
                Description = seriesInfo.Description,
                Port = port,
                Protocol = seriesInfo.IsBinaryProtocol ? "Mitsubishi MC Binary" : "Mitsubishi MC ASCII",
                Status = "Running",
                SupportedDevicesCount = simulator.GetSupportedDevices().Count,
                Series = series,
                IsOmron = false
            };
            RunningSimulators.Add(runningInfo);
            
            LogMessages.Add($"✓ {series} simulator started on port {port}");
            
            // デフォルトテストデータを設定
            await SetDefaultTestDataForMitsubishi(simulator, series);
        }
        catch (Exception ex)
        {
            LogMessages.Add($"✗ Failed to start {series} simulator: {ex.Message}");
            throw;
        }
    }

    private async Task StartOmronSimulatorInternal()
    {
        if (_runningOmronSimulator != null)
        {
            LogMessages.Add("Omron FINS simulator is already running");
            return;
        }

        try
        {
            var simulator = OmronPLCSimulatorFactory.CreateSimulator(null);
            var simulatorInfo = OmronPLCSimulatorFactory.GetSimulatorInfo();
            
            int port = Port > 0 ? Port : simulatorInfo.DefaultPort;
            
            if (IsTcpEnabled && IsUdpEnabled)
            {
                await simulator.StartBothAsync(port, UdpPort);
            }
            else if (IsUdpEnabled)
            {
                await simulator.StartUdpAsync(UdpPort);
                port = UdpPort;
            }
            else
            {
                await simulator.StartAsync(port);
            }
            
            _runningOmronSimulator = simulator;
            
            // 実行中リストに追加
            var runningInfo = new RunningSimulatorInfo
            {
                Name = "FINS",
                Description = simulatorInfo.Description,
                Port = port,
                Protocol = "Omron FINS",
                Status = "Running",
                SupportedDevicesCount = simulator.GetSupportedDevices().Count,
                Series = null,
                IsOmron = true
            };
            RunningSimulators.Add(runningInfo);
            
            LogMessages.Add($"✓ Omron FINS simulator started on port {port}");
            
            // デフォルトテストデータを設定
            await SetDefaultTestDataForOmron(simulator);
        }
        catch (Exception ex)
        {
            LogMessages.Add($"✗ Failed to start Omron FINS simulator: {ex.Message}");
            throw;
        }
    }

    private async Task StopMitsubishiSimulatorInternal(MitsubishiPLCSeries series)
    {
        if (_runningMitsubishiSimulators.TryGetValue(series, out var simulator))
        {
            try
            {
                await simulator.StopAsync();
                _runningMitsubishiSimulators.Remove(series);
                
                // 実行中リストから削除
                var runningInfo = RunningSimulators.FirstOrDefault(x => x.Series == series);
                if (runningInfo != null)
                {
                    RunningSimulators.Remove(runningInfo);
                }
                
                LogMessages.Add($"✓ {series} simulator stopped");
            }
            catch (Exception ex)
            {
                LogMessages.Add($"✗ Failed to stop {series} simulator: {ex.Message}");
                throw;
            }
        }
    }

    private async Task StopOmronSimulatorInternal()
    {
        if (_runningOmronSimulator != null)
        {
            try
            {
                await _runningOmronSimulator.StopAsync();
                _runningOmronSimulator = null;
                
                // 実行中リストから削除
                var runningInfo = RunningSimulators.FirstOrDefault(x => x.IsOmron);
                if (runningInfo != null)
                {
                    RunningSimulators.Remove(runningInfo);
                }
                
                LogMessages.Add("✓ Omron FINS simulator stopped");
            }
            catch (Exception ex)
            {
                LogMessages.Add($"✗ Failed to stop Omron FINS simulator: {ex.Message}");
                throw;
            }
        }
    }

    private Task SetDefaultTestDataForMitsubishi(MitsubishiMCSimulator simulator, MitsubishiPLCSeries series)
    {
        try
        {
            var supportedDevices = simulator.GetSupportedDevices();

            // Dレジスタのテストデータ設定
            if (supportedDevices.ContainsKey("D"))
            {
                for (int i = 0; i < 10; i++)
                {
                    var address = new PLCAddress("D", 100 + i, 1);
                    var value = BitConverter.GetBytes((ushort)(1000 + i * 111));
                    simulator.SetDeviceValue(address, value);
                }
            }

            // 内部リレーのテストデータ設定
            if (supportedDevices.ContainsKey("M"))
            {
                for (int i = 0; i < 16; i++)
                {
                    var address = new PLCAddress("M", 100 + i, 1);
                    var value = new byte[] { (byte)(i % 2), 0 };
                    simulator.SetDeviceValue(address, value);
                }
            }

            // 入力リレーのテストデータ設定
            if (supportedDevices.ContainsKey("X"))
            {
                for (int i = 0; i < 16; i++)
                {
                    var address = new PLCAddress("X", i, 1);
                    var value = new byte[] { (byte)((i % 4) == 0 ? 1 : 0), 0 };
                    simulator.SetDeviceValue(address, value);
                }
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error setting default test data: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private Task SetDefaultTestDataForOmron(OmronFINSSimulator simulator)
    {
        try
        {
            var supportedDevices = simulator.GetSupportedDevices();

            // DMレジスタのテストデータ設定
            if (supportedDevices.ContainsKey("DM"))
            {
                for (int i = 0; i < 10; i++)
                {
                    var address = new PLCAddress("DM", 100 + i, 1);
                    var value = BitConverter.GetBytes((ushort)(1000 + i * 111));
                    simulator.SetDeviceValue(address, value);
                }
            }

            // 内部補助リレーのテストデータ設定
            if (supportedDevices.ContainsKey("WR"))
            {
                for (int i = 0; i < 16; i++)
                {
                    var address = new PLCAddress("WR", 100 + i, 1);
                    var value = new byte[] { (byte)(i % 2), 0 };
                    simulator.SetDeviceValue(address, value);
                }
            }

            // 入出力リレーのテストデータ設定
            if (supportedDevices.ContainsKey("IO"))
            {
                for (int i = 0; i < 16; i++)
                {
                    var address = new PLCAddress("IO", i, 1);
                    var value = new byte[] { (byte)((i % 4) == 0 ? 1 : 0), 0 };
                    simulator.SetDeviceValue(address, value);
                }
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error setting default test data: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private Task RefreshMitsubishiDeviceValues(MitsubishiMCSimulator simulator)
    {
        try
        {
            // Dレジスタの値表示
            for (int i = 0; i < 10; i++)
            {
                var address = new PLCAddress("D", 100 + i, 1);
                var data = simulator.GetDeviceValue(address);
                if (data != null && data.Length >= 2)
                {
                    var value = BitConverter.ToUInt16(data, 0);
                    DeviceValues.Add(new DeviceValueInfo
                    {
                        DeviceAddress = $"D{100 + i}",
                        Value = value.ToString(),
                        DataType = "Word"
                    });
                }
            }

            // 内部リレーの値表示
            for (int i = 0; i < 8; i++)
            {
                var address = new PLCAddress("M", 100 + i, 1);
                var data = simulator.GetDeviceValue(address);
                if (data != null && data.Length >= 1)
                {
                    var value = data[0] != 0 ? "ON" : "OFF";
                    DeviceValues.Add(new DeviceValueInfo
                    {
                        DeviceAddress = $"M{100 + i}",
                        Value = value,
                        DataType = "Bit"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error refreshing Mitsubishi device values: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private Task RefreshOmronDeviceValues()
    {
        try
        {
            if (_runningOmronSimulator == null) return Task.CompletedTask;

            // DMレジスタの値表示
            for (int i = 0; i < 10; i++)
            {
                var address = new PLCAddress("DM", 100 + i, 1);
                var data = _runningOmronSimulator.GetDeviceValue(address);
                if (data != null && data.Length >= 2)
                {
                    var value = BitConverter.ToUInt16(data, 0);
                    DeviceValues.Add(new DeviceValueInfo
                    {
                        DeviceAddress = $"DM{100 + i}",
                        Value = value.ToString(),
                        DataType = "Word"
                    });
                }
            }

            // WRリレーの値表示
            for (int i = 0; i < 8; i++)
            {
                var address = new PLCAddress("WR", 100 + i, 1);
                var data = _runningOmronSimulator.GetDeviceValue(address);
                if (data != null && data.Length >= 1)
                {
                    var value = data[0] != 0 ? "ON" : "OFF";
                    DeviceValues.Add(new DeviceValueInfo
                    {
                        DeviceAddress = $"WR{100 + i}",
                        Value = value,
                        DataType = "Bit"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            LogMessages.Add($"Error refreshing Omron device values: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearLogs()
    {
        LogMessages.Clear();
        StatusMessage = "ログをクリアしました";
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void MinimizeWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }
    }

    [RelayCommand]
    private void MaximizeWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window != null)
            {
                window.WindowState = window.WindowState == WindowState.Maximized 
                    ? WindowState.Normal 
                    : WindowState.Maximized;
            }
        }
    }

    [RelayCommand]
    private void CloseWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}