using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PLCUnifiedSimulator.Console;

namespace PLCUnifiedSimulator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // ログ設定
        using var serviceProvider = ConfigureServices();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("PLC Unified Simulator を開始します");

        try
        {
            await PLCSimulatorConsole.RunAsync(args, serviceProvider);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PLC Unified Simulator の実行中にエラーが発生しました");
        }
        finally
        {
            logger.LogInformation("PLC Unified Simulator を終了します");
        }
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