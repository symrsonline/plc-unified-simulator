using PLCUnifiedSimulator.Console;

namespace PLCUnifiedSimulator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        await PLCSimulatorConsole.RunAsync(args);
    }
}