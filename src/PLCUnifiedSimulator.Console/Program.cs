using PLCUnifiedSimulator.Console;

namespace PLCUnifiedSimulator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        await FaEngineTestProgram.RunAsync(args);
    }
}