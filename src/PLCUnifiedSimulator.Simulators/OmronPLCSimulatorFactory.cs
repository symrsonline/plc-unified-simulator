using Microsoft.Extensions.Logging;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// オムロンPLCシミュレータファクトリ
/// </summary>
public static class OmronPLCSimulatorFactory
{
    /// <summary>
    /// オムロンFINSプロトコルシミュレータを作成
    /// </summary>
    /// <returns>シミュレータインスタンス</returns>
    public static OmronFINSSimulator CreateSimulator()
    {
        return new OmronFINSSimulator();
    }

    /// <summary>
    /// オムロンFINSプロトコルシミュレータを作成（ロガー指定）
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <returns>シミュレータインスタンス</returns>
    public static OmronFINSSimulator CreateSimulator(ILogger? logger)
    {
        return new OmronFINSSimulator(logger);
    }

    /// <summary>
    /// オムロンPLCシミュレータの情報を取得
    /// </summary>
    /// <returns>PLCシリーズの情報</returns>
    public static (string Name, string Description, int DefaultPort) GetSimulatorInfo()
    {
        return ("FINS", "オムロンFINSプロトコル", 9600);
    }
}