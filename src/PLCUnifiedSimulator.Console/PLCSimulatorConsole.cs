using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Simulators;
using System.ComponentModel;
using System.Reflection;

namespace PLCUnifiedSimulator.Console;

/// <summary>
/// PLCシミュレータコンソールプログラム
/// </summary>
public class PLCSimulatorConsole
{
    private static readonly Dictionary<MitsubishiPLCSeries, MitsubishiMCSimulator> _runningSimulators = new();
    private static OmronFINSSimulator? _runningOmronSimulator;
    private static ILogger? _logger;
    private static IServiceProvider? _serviceProvider;

    public static async Task RunAsync(string[] args, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<PLCSimulatorConsole>>();

        _logger.LogInformation("PLC Unified Simulator Console を開始します");

        System.Console.WriteLine("==============================================");
        System.Console.WriteLine("      PLC Unified Simulator Console");
        System.Console.WriteLine("==============================================");
        System.Console.WriteLine();

        try
        {
            await ShowMainMenuAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "メインメニューの実行中にエラーが発生しました");
            System.Console.WriteLine($"エラーが発生しました: {ex.Message}");
        }
        finally
        {
            await StopAllSimulatorsAsync();
            _logger.LogInformation("PLC Unified Simulator Console を終了します");
        }
    }

    private static async Task ShowMainMenuAsync()
    {
        while (true)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("メインメニュー:");
            System.Console.WriteLine("1. 利用可能なPLCシリーズ一覧表示");
            System.Console.WriteLine("2. 特定のPLCシミュレータを開始");
            System.Console.WriteLine("3. 全てのPLCシミュレータを開始");
            System.Console.WriteLine("4. オムロンFINSシミュレータを開始");
            System.Console.WriteLine("5. 実行中のシミュレータ状態表示");
            System.Console.WriteLine("6. テストデータ設定");
            System.Console.WriteLine("7. デバイス値表示");
            System.Console.WriteLine("8. シミュレータ停止");
            System.Console.WriteLine("9. 通信プロトコル設定");
            System.Console.WriteLine("0. 終了");
            System.Console.WriteLine();
            System.Console.Write("選択してください (0-9): ");

            var choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ShowAvailableSeries();
                    break;
                case "2":
                    await StartSpecificSimulatorAsync();
                    break;
                case "3":
                    await StartAllSimulatorsAsync();
                    break;
                case "4":
                    await StartOmronSimulatorAsync();
                    break;
                case "5":
                    ShowRunningSimulators();
                    break;
                case "6":
                    await SetTestDataAsync();
                    break;
                case "7":
                    await ShowDeviceValuesAsync();
                    break;
                case "8":
                    await StopSimulatorAsync();
                    break;
                case "9":
                    ShowProtocolConfiguration();
                    break;
                case "0":
                    return;
                default:
                    System.Console.WriteLine("無効な選択です。0-9の範囲で入力してください。");
                    break;
            }
        }
    }

    private static void ShowAvailableSeries()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("利用可能なPLCシリーズ:");
        System.Console.WriteLine("=" + new string('=', 80));

        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();

        foreach (var (series, description, port) in availableSeries)
        {
            var isRunning = _runningSimulators.ContainsKey(series) ? "[実行中]" : "[停止中]";
            System.Console.WriteLine($"{(int)series,2}: {series,-35} | Port:{port,5} | {description} {isRunning}");
        }

        System.Console.WriteLine("=" + new string('=', 80));
        System.Console.WriteLine($"総数: {availableSeries.Count} シリーズ");
    }

    private static async Task StartSpecificSimulatorAsync()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("開始するPLCシリーズを選択:");

        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        for (int i = 0; i < availableSeries.Count; i++)
        {
            var (series, description, port) = availableSeries[i];
            var isRunning = _runningSimulators.ContainsKey(series) ? "[実行中]" : "[停止中]";
            System.Console.WriteLine($"{i + 1,2}: {series,-35} | Port:{port,5} {isRunning}");
        }

        System.Console.Write($"選択してください (1-{availableSeries.Count}): ");

        if (int.TryParse(System.Console.ReadLine(), out var selection) &&
            selection >= 1 && selection <= availableSeries.Count)
        {
            var selectedSeries = availableSeries[selection - 1];
            await StartSimulatorAsync(selectedSeries.Series);
        }
        else
        {
            System.Console.WriteLine("無効な選択です。");
        }
    }

    private static async Task StartSimulatorAsync(MitsubishiPLCSeries series)
    {
        if (_runningSimulators.ContainsKey(series))
        {
            _logger?.LogWarning("{Series} シリーズは既に実行中です", series);
            System.Console.WriteLine($"シリーズ {series} は既に実行中です。");
            return;
        }

        try
        {
            _logger?.LogInformation("{Series} シミュレータの開始を開始します", series);
            var simulator = new MitsubishiMCSimulator(series, _logger);
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);

            await simulator.StartAsync(seriesInfo.DefaultPort);
            _runningSimulators[series] = simulator;

            _logger?.LogInformation("{Series} シミュレータがポート {Port} で正常に開始されました", series, seriesInfo.DefaultPort);
            System.Console.WriteLine($"✓ {series} シミュレータが開始されました");
            System.Console.WriteLine($"  ポート: {seriesInfo.DefaultPort}");
            System.Console.WriteLine($"  説明: {seriesInfo.Description}");
            System.Console.WriteLine($"  プロトコル: {(seriesInfo.IsBinaryProtocol ? "バイナリ" : "ASCII")}");

            // テストデータを自動設定
            await SetDefaultTestData(simulator, series);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "{Series} シミュレータの開始に失敗しました", series);
            System.Console.WriteLine($"✗ シミュレータの開始に失敗しました: {ex.Message}");
        }
    }

    private static async Task StartAllSimulatorsAsync()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("全てのPLCシミュレータを開始中...");

        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        var startTasks = new List<Task>();

        foreach (var (series, _, _) in availableSeries)
        {
            if (!_runningSimulators.ContainsKey(series))
            {
                startTasks.Add(StartSimulatorAsync(series));
            }
        }

        await Task.WhenAll(startTasks);

        System.Console.WriteLine($"完了: {_runningSimulators.Count}/{availableSeries.Count} シミュレータが実行中");
    }

    private static async Task StartOmronSimulatorAsync()
    {
        if (_runningOmronSimulator != null)
        {
            _logger?.LogWarning("オムロンFINSシミュレータは既に実行中です");
            System.Console.WriteLine("オムロンFINSシミュレータは既に実行中です。");
            return;
        }

        try
        {
            _logger?.LogInformation("オムロンFINSシミュレータの開始を開始します");
            var simulator = OmronPLCSimulatorFactory.CreateSimulator(_logger);
            var simulatorInfo = OmronPLCSimulatorFactory.GetSimulatorInfo();

            await simulator.StartAsync(simulatorInfo.DefaultPort);
            _runningOmronSimulator = simulator;

            _logger?.LogInformation("オムロンFINSシミュレータがポート {Port} で正常に開始されました", simulatorInfo.DefaultPort);
            System.Console.WriteLine($"✓ オムロンFINSシミュレータが開始されました");
            System.Console.WriteLine($"  ポート: {simulatorInfo.DefaultPort}");
            System.Console.WriteLine($"  説明: {simulatorInfo.Description}");
            System.Console.WriteLine($"  プロトコル: FINS");

            // テストデータを自動設定
            await SetDefaultTestDataForOmron(simulator);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "オムロンFINSシミュレータの開始に失敗しました");
            System.Console.WriteLine($"✗ オムロンFINSシミュレータの開始に失敗しました: {ex.Message}");
        }
    }

    private static void ShowRunningSimulators()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("実行中のシミュレータ:");
        System.Console.WriteLine("=" + new string('=', 80));

        var totalCount = _runningSimulators.Count + (_runningOmronSimulator != null ? 1 : 0);

        if (totalCount == 0)
        {
            System.Console.WriteLine("実行中のシミュレータはありません。");
            return;
        }

        foreach (var kvp in _runningSimulators)
        {
            var series = kvp.Key;
            var simulator = kvp.Value;
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);

            System.Console.WriteLine($"シリーズ: {series}");
            System.Console.WriteLine($"  ポート: {seriesInfo.DefaultPort}");
            System.Console.WriteLine($"  説明: {seriesInfo.Description}");
            System.Console.WriteLine($"  プロトコル: {(seriesInfo.IsBinaryProtocol ? "バイナリ" : "ASCII")}");
            System.Console.WriteLine($"  サポートデバイス数: {simulator.GetSupportedDevices().Count}");
            System.Console.WriteLine();
        }

        if (_runningOmronSimulator != null)
        {
            var simulatorInfo = OmronPLCSimulatorFactory.GetSimulatorInfo();
            System.Console.WriteLine($"シリーズ: オムロンFINS");
            System.Console.WriteLine($"  ポート: {simulatorInfo.DefaultPort}");
            System.Console.WriteLine($"  説明: {simulatorInfo.Description}");
            System.Console.WriteLine($"  プロトコル: FINS");
            System.Console.WriteLine($"  サポートデバイス数: {_runningOmronSimulator.GetSupportedDevices().Count}");
            System.Console.WriteLine();
        }

        System.Console.WriteLine($"総実行数: {totalCount} シミュレータ");
    }

    private static Task SetDefaultTestData(MitsubishiMCSimulator simulator, MitsubishiPLCSeries series)
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

            System.Console.WriteLine($"  デフォルトテストデータを設定しました (D100-109, M100-115, X0-15)");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  テストデータ設定エラー: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static Task SetDefaultTestDataForOmron(OmronFINSSimulator simulator)
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

            System.Console.WriteLine($"  デフォルトテストデータを設定しました (DM100-109, WR100-115, IO0-15)");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  テストデータ設定エラー: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static async Task SetTestDataAsync()
    {
        if (_runningSimulators.Count == 0)
        {
            System.Console.WriteLine("実行中のシミュレータがありません。");
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("テストデータを設定するシミュレータを選択:");

        var simulatorList = _runningSimulators.ToList();
        for (int i = 0; i < simulatorList.Count; i++)
        {
            System.Console.WriteLine($"{i + 1}: {simulatorList[i].Key}");
        }

        System.Console.Write($"選択してください (1-{simulatorList.Count}): ");

        if (!int.TryParse(System.Console.ReadLine(), out var selection) ||
            selection < 1 || selection > simulatorList.Count)
        {
            System.Console.WriteLine("無効な選択です。");
            return;
        }

        var selectedSimulator = simulatorList[selection - 1];
        await SetCustomTestDataAsync(selectedSimulator.Value, selectedSimulator.Key);
    }

    private static Task SetCustomTestDataAsync(MitsubishiMCSimulator simulator, MitsubishiPLCSeries series)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("カスタムテストデータ設定:");
        System.Console.WriteLine($"対象シリーズ: {series}");

        var supportedDevices = simulator.GetSupportedDevices();
        System.Console.WriteLine("サポートされているデバイス:");
        foreach (var device in supportedDevices)
        {
            var deviceType = device.Value.IsWordDevice ? "ワード" : "ビット";
            System.Console.WriteLine($"  {device.Key}: {deviceType}デバイス");
        }

        System.Console.Write("デバイスタイプ (例: D): ");
        var deviceTypeInput = System.Console.ReadLine()?.ToUpper();

        if (string.IsNullOrEmpty(deviceTypeInput) || !supportedDevices.ContainsKey(deviceTypeInput))
        {
            System.Console.WriteLine("サポートされていないデバイスタイプです。");
            return Task.CompletedTask;
        }

        System.Console.Write("開始アドレス (例: 100): ");
        if (!int.TryParse(System.Console.ReadLine(), out var startAddress))
        {
            System.Console.WriteLine("無効なアドレスです。");
            return Task.CompletedTask;
        }

        System.Console.Write("値 (例: 1234): ");
        if (!int.TryParse(System.Console.ReadLine(), out var value))
        {
            System.Console.WriteLine("無効な値です。");
            return Task.CompletedTask;
        }

        try
        {
            var address = new PLCAddress(deviceTypeInput, startAddress, 1);
            var data = BitConverter.GetBytes((ushort)value);
            simulator.SetDeviceValue(address, data);

            System.Console.WriteLine($"✓ {deviceTypeInput}{startAddress} に値 {value} を設定しました");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"✗ データ設定エラー: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static Task ShowDeviceValuesAsync()
    {
        if (_runningSimulators.Count == 0)
        {
            System.Console.WriteLine("実行中のシミュレータがありません。");
            return Task.CompletedTask;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("デバイス値を表示するシミュレータを選択:");

        var simulatorList = _runningSimulators.ToList();
        for (int i = 0; i < simulatorList.Count; i++)
        {
            System.Console.WriteLine($"{i + 1}: {simulatorList[i].Key}");
        }

        System.Console.Write($"選択してください (1-{simulatorList.Count}): ");

        if (!int.TryParse(System.Console.ReadLine(), out var selection) ||
            selection < 1 || selection > simulatorList.Count)
        {
            System.Console.WriteLine("無効な選択です。");
            return Task.CompletedTask;
        }

        var selectedSimulator = simulatorList[selection - 1];
        ShowDeviceValues(selectedSimulator.Value, selectedSimulator.Key);
        return Task.CompletedTask;
    }

    private static void ShowDeviceValues(MitsubishiMCSimulator simulator, MitsubishiPLCSeries series)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"デバイス値表示 - {series}:");
        System.Console.WriteLine("=" + new string('=', 60));

        // Dレジスタの値表示
        System.Console.WriteLine("Dレジスタ (D100-109):");
        for (int i = 0; i < 10; i++)
        {
            var address = new PLCAddress("D", 100 + i, 1);
            var data = simulator.GetDeviceValue(address);
            if (data != null && data.Length >= 2)
            {
                var value = BitConverter.ToUInt16(data, 0);
                System.Console.WriteLine($"  D{100 + i}: {value}");
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine("内部リレー (M100-107):");
        for (int i = 0; i < 8; i++)
        {
            var address = new PLCAddress("M", 100 + i, 1);
            var data = simulator.GetDeviceValue(address);
            if (data != null && data.Length >= 1)
            {
                var value = data[0] != 0 ? "ON" : "OFF";
                System.Console.WriteLine($"  M{100 + i}: {value}");
            }
        }
    }

    private static async Task StopSimulatorAsync()
    {
        if (_runningSimulators.Count == 0)
        {
            System.Console.WriteLine("実行中のシミュレータがありません。");
            return;
        }

        System.Console.WriteLine();
        System.Console.WriteLine("停止するシミュレータを選択:");
        System.Console.WriteLine("0: 全て停止");

        var simulatorList = _runningSimulators.ToList();
        for (int i = 0; i < simulatorList.Count; i++)
        {
            System.Console.WriteLine($"{i + 1}: {simulatorList[i].Key}");
        }

        System.Console.Write($"選択してください (0-{simulatorList.Count}): ");

        if (!int.TryParse(System.Console.ReadLine(), out var selection) ||
            selection < 0 || selection > simulatorList.Count)
        {
            System.Console.WriteLine("無効な選択です。");
            return;
        }

        if (selection == 0)
        {
            await StopAllSimulatorsAsync();
        }
        else
        {
            var selectedSimulator = simulatorList[selection - 1];
            await StopSpecificSimulatorAsync(selectedSimulator.Key);
        }
    }

    private static async Task StopSpecificSimulatorAsync(MitsubishiPLCSeries series)
    {
        if (_runningSimulators.TryGetValue(series, out var simulator))
        {
            try
            {
                _logger?.LogInformation("{Series} シミュレータの停止を開始します", series);
                await simulator.StopAsync();
                _runningSimulators.Remove(series);
                _logger?.LogInformation("{Series} シミュレータが正常に停止されました", series);
                System.Console.WriteLine($"✓ {series} シミュレータを停止しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "{Series} シミュレータの停止に失敗しました", series);
                System.Console.WriteLine($"✗ シミュレータの停止に失敗しました: {ex.Message}");
            }
        }
        else
        {
            _logger?.LogWarning("{Series} シミュレータは実行中ではありません", series);
        }
    }

    private static async Task StopAllSimulatorsAsync()
    {
        System.Console.WriteLine("全てのシミュレータを停止中...");

        var stopTasks = new List<Task>();
        foreach (var kvp in _runningSimulators.ToList())
        {
            stopTasks.Add(StopSpecificSimulatorAsync(kvp.Key));
        }

        if (_runningOmronSimulator != null)
        {
            stopTasks.Add(StopOmronSimulatorAsync());
        }

        await Task.WhenAll(stopTasks);

        System.Console.WriteLine("✓ 全てのシミュレータを停止しました");
    }

    private static async Task StopOmronSimulatorAsync()
    {
        if (_runningOmronSimulator != null)
        {
            try
            {
                _logger?.LogInformation("オムロンFINSシミュレータの停止を開始します");
                await _runningOmronSimulator.StopAsync();
                _runningOmronSimulator = null;
                _logger?.LogInformation("オムロンFINSシミュレータが正常に停止されました");
                System.Console.WriteLine($"✓ オムロンFINSシミュレータを停止しました");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "オムロンFINSシミュレータの停止に失敗しました");
                System.Console.WriteLine($"✗ オムロンFINSシミュレータの停止に失敗しました: {ex.Message}");
            }
        }
        else
        {
            _logger?.LogWarning("オムロンFINSシミュレータは実行中ではありません");
        }
    }

    private static void ShowProtocolConfiguration()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("通信プロトコル設定ガイド:");
        System.Console.WriteLine("=" + new string('=', 80));
        System.Console.WriteLine();

        System.Console.WriteLine("1. TCP通信設定:");
        System.Console.WriteLine("   - 三菱MCプロトコル (バイナリ)");
        System.Console.WriteLine("     IPアドレス: 127.0.0.1, ポート: 5000-5002");
        System.Console.WriteLine("   - 三菱MCプロトコル (ASCII)");
        System.Console.WriteLine("     IPアドレス: 127.0.0.1, ポート: 5010-5012");
        System.Console.WriteLine("   - オムロンFINSプロトコル");
        System.Console.WriteLine("     IPアドレス: 127.0.0.1, ポート: 9600");
        System.Console.WriteLine();

        System.Console.WriteLine("2. UDP通信設定:");
        System.Console.WriteLine("   - 三菱MCプロトコル (UDP対応)");
        System.Console.WriteLine("     IPアドレス: 127.0.0.1, ポート: 5100-5112");
        System.Console.WriteLine("   - オムロンFINSプロトコル (UDP対応)");
        System.Console.WriteLine("     IPアドレス: 127.0.0.1, ポート: 9700");
        System.Console.WriteLine();

        System.Console.WriteLine("3. テスト用デバイスアドレス:");
        System.Console.WriteLine("   - データレジスタ: D100-D109 (値: 1000, 1111, 1222, ...)");
        System.Console.WriteLine("   - 内部リレー: M100-M115 (交互にON/OFF)");
        System.Console.WriteLine("   - 入力リレー: X0-X15 (4つおきにON)");
        System.Console.WriteLine();

        System.Console.WriteLine("4. 各PLCシリーズのポート番号:");
        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        foreach (var (series, description, port) in availableSeries.Take(10))
        {
            var udpPort = port + 100; // UDP port is TCP port + 100
            System.Console.WriteLine($"   - {series}: TCP {port}, UDP {udpPort}");
        }
        if (availableSeries.Count > 10)
        {
            System.Console.WriteLine($"   ... 他 {availableSeries.Count - 10} シリーズ");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("5. 接続テスト手順:");
        System.Console.WriteLine("   1) 上記メニューでシミュレータを開始");
        System.Console.WriteLine("   2) クライアントで対応するプロトコルと設定を選択");
        System.Console.WriteLine("   3) TCP/UDP通信方式を選択");
        System.Console.WriteLine("   4) 通信テストを実行");
        System.Console.WriteLine("   5) デバイス読み取り/書き込みテストを実行");
        System.Console.WriteLine();
    }

    private static string GetDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}