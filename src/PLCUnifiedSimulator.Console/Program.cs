using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PLCUnifiedSimulator.Console;

namespace PLCUnifiedSimulator.Console;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // ログ設定
        using var serviceProvider = ConfigureServices();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // コマンドライン引数の解析
        if (args.Length == 0)
        {
            // デフォルト：インタラクティブモード
            logger.LogInformation("PLC Unified Simulator を開始します");
            try
            {
                await PLCSimulatorConsole.RunInteractiveAsync(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PLC Unified Simulator の実行中にエラーが発生しました");
                return 1;
            }
            finally
            {
                logger.LogInformation("PLC Unified Simulator を終了します");
            }
            return 0;
        }

        // ヘルプ表示
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
        {
            ShowUsage();
            return 0;
        }

        // コマンド解析
        var command = args[0].ToLower();

        try
        {
            switch (command)
            {
                case "interactive":
                    logger.LogInformation("PLC Unified Simulator をインタラクティブモードで開始します");
                    await PLCSimulatorConsole.RunInteractiveAsync(serviceProvider);
                    break;

                case "mitsubishi":
                    var mitsubishiArgs = ParseMitsubishiArgs(args);
                    logger.LogInformation("三菱MCプロトコルシミュレータを開始します - ポート: {Port}, シリーズ: {Series}",
                        mitsubishiArgs.Port, mitsubishiArgs.Series);
                    await PLCSimulatorConsole.RunMitsubishiSimulatorAsync(mitsubishiArgs.Port, mitsubishiArgs.Series, serviceProvider);
                    break;

                case "omron":
                    var omronPort = ParseOmronArgs(args);
                    logger.LogInformation("オムロンFINSプロトコルシミュレータを開始します - ポート: {Port}", omronPort);
                    await PLCSimulatorConsole.RunOmronSimulatorAsync(omronPort, serviceProvider);
                    break;

                default:
                    System.Console.WriteLine($"不明なコマンド: {command}");
                    ShowUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "コマンド実行中にエラーが発生しました");
            System.Console.WriteLine($"エラーが発生しました: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static (int Port, string Series) ParseMitsubishiArgs(string[] args)
    {
        int port = 5000;
        string series = "QJ71E71_Binary_Station1";

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                    {
                        port = p;
                        i++; // 次の引数をスキップ
                    }
                    else
                    {
                        throw new ArgumentException("Invalid port number");
                    }
                    break;

                case "--series":
                    if (i + 1 < args.Length)
                    {
                        series = args[i + 1];
                        i++; // 次の引数をスキップ
                    }
                    else
                    {
                        throw new ArgumentException("Series name is required");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }

        return (port, series);
    }

    private static int ParseOmronArgs(string[] args)
    {
        int port = 9600;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--port":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var p))
                    {
                        port = p;
                        i++; // 次の引数をスキップ
                    }
                    else
                    {
                        throw new ArgumentException("Invalid port number");
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }

        return port;
    }

    private static void ShowUsage()
    {
        System.Console.WriteLine("PLC Unified Simulator Console Application");
        System.Console.WriteLine();
        System.Console.WriteLine("USAGE:");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console [command] [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("COMMANDS:");
        System.Console.WriteLine("  (no command)      Run in interactive mode (default)");
        System.Console.WriteLine("  interactive       Run in interactive mode");
        System.Console.WriteLine("  mitsubishi        Start Mitsubishi MC Protocol simulator");
        System.Console.WriteLine("  omron             Start Omron FINS Protocol simulator");
        System.Console.WriteLine("  help, --help, -h  Show this help information");
        System.Console.WriteLine();
        System.Console.WriteLine("MITSUBISHI OPTIONS:");
        System.Console.WriteLine("  --port <port>     Port number to listen on (default: 5000)");
        System.Console.WriteLine("  --series <series> PLC series to simulate (default: QJ71E71_Binary_Station1)");
        System.Console.WriteLine();
        System.Console.WriteLine("OMRON OPTIONS:");
        System.Console.WriteLine("  --port <port>     Port number to listen on (default: 9600)");
        System.Console.WriteLine();
        System.Console.WriteLine("AVAILABLE PLC SERIES:");
        var availableSeries = PLCUnifiedSimulator.Simulators.MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        foreach (var (series, description, port) in availableSeries)
        {
            System.Console.WriteLine($"  {series,-35} - {description}");
        }
        System.Console.WriteLine();
        System.Console.WriteLine("EXAMPLES:");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console interactive");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console mitsubishi --port 5001");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console mitsubishi --series FX3U_ENET --port 5002");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console omron --port 9601");
        System.Console.WriteLine("  PLCUnifiedSimulator.Console --help");
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // ログ設定
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.Services.Configure<ConsoleFormatterOptions>(options =>
            {
                options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
            });
            configure.SetMinimumLevel(LogLevel.Information);
        });

        return services.BuildServiceProvider();
    }
}