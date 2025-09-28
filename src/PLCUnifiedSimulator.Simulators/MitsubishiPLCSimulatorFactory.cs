using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// 三菱PLCシミュレータファクトリ
/// </summary>
public static class MitsubishiPLCSimulatorFactory
{
    /// <summary>
    /// 指定されたシリーズの三菱PLCシミュレータを作成
    /// </summary>
    /// <param name="series">PLCシリーズ</param>
    /// <returns>シミュレータインスタンス</returns>
    public static MitsubishiMCSimulator CreateSimulator(MitsubishiPLCSeries series)
    {
        return new MitsubishiMCSimulator(series);
    }

    /// <summary>
    /// すべてのサポートされているPLCシリーズのシミュレータを作成
    /// </summary>
    /// <returns>シミュレータのディクショナリ</returns>
    public static Dictionary<MitsubishiPLCSeries, MitsubishiMCSimulator> CreateAllSimulators()
    {
        var simulators = new Dictionary<MitsubishiPLCSeries, MitsubishiMCSimulator>();
        
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            simulators[series] = CreateSimulator(series);
        }
        
        return simulators;
    }

    /// <summary>
    /// シリーズ名からシミュレータを作成
    /// </summary>
    /// <param name="seriesName">シリーズ名</param>
    /// <returns>シミュレータインスタンス</returns>
    /// <exception cref="ArgumentException">無効なシリーズ名の場合</exception>
    public static MitsubishiMCSimulator CreateSimulatorByName(string seriesName)
    {
        if (Enum.TryParse<MitsubishiPLCSeries>(seriesName, true, out var series))
        {
            return CreateSimulator(series);
        }
        
        throw new ArgumentException($"無効なPLCシリーズ名です: {seriesName}", nameof(seriesName));
    }

    /// <summary>
    /// 利用可能なPLCシリーズの一覧を取得
    /// </summary>
    /// <returns>PLCシリーズの情報リスト</returns>
    public static List<(MitsubishiPLCSeries Series, string Description, int Port)> GetAvailableSeries()
    {
        var seriesList = new List<(MitsubishiPLCSeries Series, string Description, int Port)>();
        
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var info = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            seriesList.Add((series, info.Description, info.DefaultPort));
        }
        
        return seriesList.OrderBy(x => x.Port).ToList();
    }
}