using Xunit;
using FluentAssertions;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Simulators;
using System.ComponentModel;
using System.Reflection;

namespace PLCUnifiedSimulator.Tests;

public class MitsubishiAllSeriesXUnitTests
{
    [Fact]
    public void AllSeriesEnumValues_ShouldHaveDescriptions()
    {
        // すべてのPLCシリーズ列挙値が適切に定義されているかテスト
        var seriesValues = Enum.GetValues<MitsubishiPLCSeries>();
        seriesValues.Length.Should().BeGreaterOrEqualTo(12, "12個以上のPLCシリーズが定義されている必要があります");

        foreach (var series in seriesValues)
        {
            // 各シリーズにDescriptionAttributeが設定されているかチェック
            var field = typeof(MitsubishiPLCSeries).GetField(series.ToString());
            var descriptionAttr = field?.GetCustomAttribute<DescriptionAttribute>();
            descriptionAttr.Should().NotBeNull($"シリーズ {series} にDescription属性が設定されている必要があります");
            descriptionAttr!.Description.Should().NotBeNullOrEmpty($"シリーズ {series} の説明が空です");
        }
    }

    [Fact]
    public void SeriesInfo_ShouldBeCreatedForAllSeries()
    {
        // すべてのPLCシリーズで適切なSeriesInfoが作成できるかテスト
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            
            seriesInfo.Should().NotBeNull($"シリーズ {series} のSeriesInfoが取得できません");
            seriesInfo.Series.Should().Be(series, $"シリーズ {series} のSeries値が一致しません");
            seriesInfo.DefaultPort.Should().BePositive($"シリーズ {series} のデフォルトポートが無効です");
            seriesInfo.Description.Should().NotBeNullOrEmpty($"シリーズ {series} の説明が空です");
            seriesInfo.SupportedDevices.Should().NotBeEmpty($"シリーズ {series} でサポートされているデバイスがありません");
        }
    }

    [Fact]
    public void DefaultPorts_ShouldBeUnique()
    {
        // すべてのPLCシリーズで異なるデフォルトポートが設定されているかテスト
        var ports = new HashSet<int>();
        
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            ports.Add(seriesInfo.DefaultPort).Should().BeTrue($"シリーズ {series} のポート {seriesInfo.DefaultPort} が重複しています");
        }
    }

    [Fact]
    public void BinaryAndASCIIProtocols_ShouldBeClassifiedCorrectly()
    {
        // バイナリとASCIIプロトコルが適切に分類されているかテスト
        var binaryProtocols = new List<MitsubishiPLCSeries>();
        var asciiProtocols = new List<MitsubishiPLCSeries>();
        
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            
            if (seriesInfo.IsBinaryProtocol)
                binaryProtocols.Add(series);
            else
                asciiProtocols.Add(series);
        }
        
        binaryProtocols.Should().NotBeEmpty("バイナリプロトコルのシリーズが存在する必要があります");
        asciiProtocols.Should().NotBeEmpty("ASCIIプロトコルのシリーズが存在する必要があります");
        
        // 特定のシリーズがバイナリプロトコルであることを確認
        binaryProtocols.Should().Contain(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        binaryProtocols.Should().Contain(MitsubishiPLCSeries.FX5U_CPU_Binary);
        
        // 特定のシリーズがASCIIプロトコルであることを確認
        asciiProtocols.Should().Contain(MitsubishiPLCSeries.QJ71E71_ASCII_Station1);
        asciiProtocols.Should().Contain(MitsubishiPLCSeries.FX5U_CPU_ASCII);
    }

    [Fact]
    public void SupportedDevices_ShouldIncludeBasicDevices()
    {
        // 各シリーズで適切なデバイスがサポートされているかテスト
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            var supportedDevices = seriesInfo.SupportedDevices;
            
            // すべてのシリーズで基本的なデバイスがサポートされていることを確認
            supportedDevices.Should().ContainKey("D", $"シリーズ {series} でDレジスタがサポートされている必要があります");
            supportedDevices.Should().ContainKey("X", $"シリーズ {series} で入力リレーがサポートされている必要があります");
            supportedDevices.Should().ContainKey("Y", $"シリーズ {series} で出力リレーがサポートされている必要があります");
            supportedDevices.Should().ContainKey("M", $"シリーズ {series} で内部リレーがサポートされている必要があります");
            
            // Dレジスタがワードデバイスであることを確認
            supportedDevices["D"].IsWordDevice.Should().BeTrue($"シリーズ {series} でDレジスタはワードデバイスである必要があります");
            
            // リレー類がビットデバイスであることを確認
            supportedDevices["X"].IsWordDevice.Should().BeFalse($"シリーズ {series} で入力リレーはビットデバイスである必要があります");
            supportedDevices["Y"].IsWordDevice.Should().BeFalse($"シリーズ {series} で出力リレーはビットデバイスである必要があります");
            supportedDevices["M"].IsWordDevice.Should().BeFalse($"シリーズ {series} で内部リレーはビットデバイスである必要があります");
        }
    }

    [Fact]
    public void QLiQRSeries_ShouldHaveSpecificDevices()
    {
        // Q/L/iQ-Rシリーズ特有のデバイステスト
        var qliQRSeries = new[]
        {
            MitsubishiPLCSeries.QJ71E71_Binary_Station1,
            MitsubishiPLCSeries.QJ71E71_Binary_Station2,
            MitsubishiPLCSeries.QJ71E71_Binary_Station3,
            MitsubishiPLCSeries.QJ71E71_ASCII_Station1,
            MitsubishiPLCSeries.QJ71E71_ASCII_Station2,
            MitsubishiPLCSeries.QJ71E71_ASCII_Station3
        };
        
        foreach (var series in qliQRSeries)
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            var supportedDevices = seriesInfo.SupportedDevices;
            
            // Q/L/iQ-Rシリーズ特有のデバイスをテスト
            supportedDevices.Should().ContainKey("ZR", $"Q/L/iQ-Rシリーズ {series} でZRレジスタがサポートされている必要があります");
            supportedDevices.Should().ContainKey("L", $"Q/L/iQ-Rシリーズ {series} でLリレーがサポートされている必要があります");
            supportedDevices.Should().ContainKey("F", $"Q/L/iQ-Rシリーズ {series} でFリレーがサポートされている必要があります");
        }
    }

    [Fact]
    public void FX5USeries_ShouldHaveSpecificDevices()
    {
        // FX5Uシリーズ特有のデバイステスト
        var fx5uSeries = new[]
        {
            MitsubishiPLCSeries.FX5U_CPU_Binary,
            MitsubishiPLCSeries.FX5U_CPU_ASCII
        };
        
        foreach (var series in fx5uSeries)
        {
            var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
            var supportedDevices = seriesInfo.SupportedDevices;
            
            // FX5Uシリーズ特有のデバイスをテスト
            supportedDevices.Should().ContainKey("SM", $"FX5Uシリーズ {series} でSMリレーがサポートされている必要があります");
            supportedDevices.Should().ContainKey("SD", $"FX5Uシリーズ {series} でSDレジスタがサポートされている必要があります");
        }
    }

    [Fact]
    public void Protocol_ShouldBeCreatedForAllSeries()
    {
        // 各シリーズでプロトコルインスタンスが正常に作成できるかテスト
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var protocol = new MitsubishiMCProtocol(series);
            
            protocol.Should().NotBeNull($"シリーズ {series} のプロトコルが作成できません");
            protocol.PLCSeries.Should().Be(series, $"シリーズ {series} のPLCSeriesプロパティが一致しません");
            protocol.DefaultPort.Should().BePositive($"シリーズ {series} のデフォルトポートが無効です");
            protocol.ProtocolName.Should().NotBeNullOrEmpty($"シリーズ {series} のプロトコル名が空です");
        }
    }

    [Fact]
    public void Simulator_ShouldBeCreatedForAllSeries()
    {
        // 各シリーズでシミュレータインスタンスが正常に作成できるかテスト
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var simulator = new MitsubishiMCSimulator(series);
            
            simulator.Should().NotBeNull($"シリーズ {series} のシミュレータが作成できません");
            simulator.PLCSeries.Should().Be(series, $"シリーズ {series} のPLCSeriesプロパティが一致しません");
            simulator.Protocol.Should().NotBeNull($"シリーズ {series} のプロトコルが取得できません");
            simulator.GetSeriesDescription().Should().NotBeNullOrEmpty($"シリーズ {series} の説明が空です");
        }
    }

    [Fact]
    public void Factory_ShouldCreateAllSimulators()
    {
        // ファクトリメソッドのテスト
        var allSimulators = MitsubishiPLCSimulatorFactory.CreateAllSimulators();
        
        allSimulators.Should().HaveCountGreaterOrEqualTo(12, "12個以上のシミュレータが作成される必要があります");
        
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            allSimulators.Should().ContainKey(series, $"シリーズ {series} のシミュレータが含まれていません");
        }
        
        // 個別作成のテスト
        var individualSimulator = MitsubishiPLCSimulatorFactory.CreateSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        individualSimulator.Should().NotBeNull();
        individualSimulator.PLCSeries.Should().Be(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        
        // 名前による作成のテスト
        var namedSimulator = MitsubishiPLCSimulatorFactory.CreateSimulatorByName("QJ71E71_Binary_Station1");
        namedSimulator.Should().NotBeNull();
        namedSimulator.PLCSeries.Should().Be(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
    }

    [Fact]
    public void Factory_GetAvailableSeries_ShouldReturnSortedList()
    {
        // 利用可能シリーズの取得テスト
        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();
        availableSeries.Should().HaveCountGreaterOrEqualTo(12, "12個以上の利用可能シリーズが取得される必要があります");
        
        // ポート番号でソートされているかチェック
        for (int i = 0; i < availableSeries.Count - 1; i++)
        {
            availableSeries[i].Port.Should().BeLessOrEqualTo(availableSeries[i + 1].Port, "利用可能シリーズがポート番号順にソートされていません");
        }
    }

    [Fact]
    public void DeviceInfo_ShouldBeRetrievableForAllSeries()
    {
        // デバイス情報取得のテスト
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            var protocol = new MitsubishiMCProtocol(series);
            var supportedDevices = protocol.GetSupportedDevices();
            
            supportedDevices.Should().NotBeEmpty($"シリーズ {series} でサポートされているデバイスがありません");
            
            foreach (var device in supportedDevices)
            {
                // デバイスコードが有効範囲内にあることを確認
                device.Value.Code.Should().BeInRange((byte)0x01, (byte)0xFE, $"シリーズ {series} のデバイス {device.Key} のコードが無効です");
                
                // デバイス名が空でないことを確認
                device.Key.Should().NotBeNullOrEmpty($"シリーズ {series} でデバイス名が空です");
                
                // ワードデバイス判定のテスト
                var isWordDevice = protocol.IsWordDevice(device.Key);
                isWordDevice.Should().Be(device.Value.IsWordDevice, $"シリーズ {series} のデバイス {device.Key} のワードデバイス判定が一致しません");
            }
        }
    }

    [Fact]
    public async Task Simulator_ShouldStartAndStop()
    {
        // シミュレータの開始・停止テスト
        var series = MitsubishiPLCSeries.QJ71E71_Binary_Station1;
        var simulator = new MitsubishiMCSimulator(series);
        
        simulator.IsRunning.Should().BeFalse("シミュレータは初期状態で停止している必要があります");
        
        // 開始テスト
        var seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
        await simulator.StartAsync(seriesInfo.DefaultPort);
        
        simulator.IsRunning.Should().BeTrue("シミュレータが正常に開始されませんでした");
        
        // 停止テスト
        await simulator.StopAsync();
        
        simulator.IsRunning.Should().BeFalse("シミュレータが正常に停止されませんでした");
    }

    [Fact]
    public void Factory_ShouldHandleInvalidSeriesName()
    {
        // エラーハンドリングのテスト
        Action act = () => MitsubishiPLCSimulatorFactory.CreateSimulatorByName("InvalidSeriesName");
        act.Should().Throw<ArgumentException>().WithMessage("*無効なPLCシリーズ名*");
    }

    [Fact]
    public void SeriesInfo_ShouldThrowForInvalidSeries()
    {
        // 無効なシリーズでの SeriesInfo 取得テスト（最大値 + 1）
        var maxValue = Enum.GetValues<MitsubishiPLCSeries>().Max();
        var invalidSeries = (MitsubishiPLCSeries)((int)maxValue + 1);
        
        Action act = () => MitsubishiPLCSeriesInfo.GetSeriesInfo(invalidSeries);
        act.Should().Throw<NotSupportedException>().WithMessage("*未対応のPLCシリーズ*");
    }
}