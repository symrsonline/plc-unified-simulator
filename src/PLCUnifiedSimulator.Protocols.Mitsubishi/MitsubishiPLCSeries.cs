using System.ComponentModel;

namespace PLCUnifiedSimulator.Protocols.Mitsubishi;

/// <summary>
/// 三菱PLCシリーズの定義
/// </summary>
public enum MitsubishiPLCSeries
{
    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (QJ71E71相当バイナリプロトコル、推奨)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (QJ71E71相当バイナリプロトコル、推奨)")]
    QJ71E71_Binary_Station1,

    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (QJ71E71相当バイナリプロトコル、推奨)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (QJ71E71相当バイナリプロトコル、推奨)")]
    QJ71E71_Binary_Station2,

    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (QJ71E71相当バイナリプロトコル、推奨)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (QJ71E71相当バイナリプロトコル、推奨)")]
    QJ71E71_Binary_Station3,

    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (QJ71E71相当アスキープロトコル)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (QJ71E71相当アスキープロトコル)")]
    QJ71E71_ASCII_Station1,

    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (QJ71E71相当アスキープロトコル)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (QJ71E71相当アスキープロトコル)")]
    QJ71E71_ASCII_Station2,

    /// <summary>
    /// MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (QJ71E71相当アスキープロトコル)
    /// </summary>
    [Description("MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (QJ71E71相当アスキープロトコル)")]
    QJ71E71_ASCII_Station3,

    /// <summary>
    /// MELSEC iQ-Fシリーズ FX5U/FX5UC/FX5UJ (CPUポートバイナリプロトコル、推奨)
    /// </summary>
    [Description("MELSEC iQ-Fシリーズ FX5U/FX5UC/FX5UJ (CPUポートバイナリプロトコル、推奨)")]
    FX5U_CPU_Binary,

    /// <summary>
    /// MELSEC iQ-Fシリーズ FX5U/FX5UC/FX5UJ (CPUポートアスキープロトコル)
    /// </summary>
    [Description("MELSEC iQ-Fシリーズ FX5U/FX5UC/FX5UJ (CPUポートアスキープロトコル)")]
    FX5U_CPU_ASCII,

    /// <summary>
    /// MELSEC-QnAシリーズ アクセス局1 (AJ71QE71相当)
    /// </summary>
    [Description("MELSEC-QnAシリーズ アクセス局1 (AJ71QE71相当)")]
    AJ71QE71_Station1,

    /// <summary>
    /// MELSEC-QnAシリーズ アクセス局2 (AJ71QE71相当)
    /// </summary>
    [Description("MELSEC-QnAシリーズ アクセス局2 (AJ71QE71相当)")]
    AJ71QE71_Station2,

    /// <summary>
    /// MELSEC-Aシリーズ (AJ71E71相当)
    /// </summary>
    [Description("MELSEC-Aシリーズ (AJ71E71相当)")]
    AJ71E71,

    /// <summary>
    /// MELSEC-FXシリーズ (FX3U-ENET-L/FX3U-ENET-ADP)
    /// </summary>
    [Description("MELSEC-FXシリーズ (FX3U-ENET-L/FX3U-ENET-ADP)")]
    FX3U_ENET
}

/// <summary>
/// 三菱PLCシリーズ設定情報
/// </summary>
public class MitsubishiPLCSeriesInfo
{
    public MitsubishiPLCSeries Series { get; set; }
    public int DefaultPort { get; set; }
    public bool IsBinaryProtocol { get; set; }
    public byte NetworkNumber { get; set; }
    public byte StationNumber { get; set; }
    public ushort ModuleIONumber { get; set; }
    public byte MultiDropStationNumber { get; set; }
    public Dictionary<string, (byte Code, bool IsWordDevice)> SupportedDevices { get; set; } = new();
    public string Description { get; set; } = string.Empty;

    public static MitsubishiPLCSeriesInfo GetSeriesInfo(MitsubishiPLCSeries series)
    {
        return series switch
        {
            // Q/L/iQ-Rシリーズ (バイナリプロトコル)
            MitsubishiPLCSeries.QJ71E71_Binary_Station1 => new()
            {
                Series = series,
                DefaultPort = 5000,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (バイナリ)",
                SupportedDevices = GetQLiQRDevices()
            },
            MitsubishiPLCSeries.QJ71E71_Binary_Station2 => new()
            {
                Series = series,
                DefaultPort = 5001,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (バイナリ)",
                SupportedDevices = GetQLiQRDevices()
            },
            MitsubishiPLCSeries.QJ71E71_Binary_Station3 => new()
            {
                Series = series,
                DefaultPort = 5002,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (バイナリ)",
                SupportedDevices = GetQLiQRDevices()
            },

            // Q/L/iQ-Rシリーズ (ASCIIプロトコル)
            MitsubishiPLCSeries.QJ71E71_ASCII_Station1 => new()
            {
                Series = series,
                DefaultPort = 5010,
                IsBinaryProtocol = false,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局1 (ASCII)",
                SupportedDevices = GetQLiQRDevices()
            },
            MitsubishiPLCSeries.QJ71E71_ASCII_Station2 => new()
            {
                Series = series,
                DefaultPort = 5011,
                IsBinaryProtocol = false,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局2 (ASCII)",
                SupportedDevices = GetQLiQRDevices()
            },
            MitsubishiPLCSeries.QJ71E71_ASCII_Station3 => new()
            {
                Series = series,
                DefaultPort = 5012,
                IsBinaryProtocol = false,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Q/L/iQ-Rシリーズ アクセス局3 (ASCII)",
                SupportedDevices = GetQLiQRDevices()
            },

            // iQ-Fシリーズ FX5U
            MitsubishiPLCSeries.FX5U_CPU_Binary => new()
            {
                Series = series,
                DefaultPort = 5020,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC iQ-Fシリーズ FX5U (CPUポートバイナリ)",
                SupportedDevices = GetFX5UDevices()
            },
            MitsubishiPLCSeries.FX5U_CPU_ASCII => new()
            {
                Series = series,
                DefaultPort = 5021,
                IsBinaryProtocol = false,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC iQ-Fシリーズ FX5U (CPUポートASCII)",
                SupportedDevices = GetFX5UDevices()
            },

            // QnAシリーズ
            MitsubishiPLCSeries.AJ71QE71_Station1 => new()
            {
                Series = series,
                DefaultPort = 5030,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-QnAシリーズ アクセス局1",
                SupportedDevices = GetQnADevices()
            },
            MitsubishiPLCSeries.AJ71QE71_Station2 => new()
            {
                Series = series,
                DefaultPort = 5031,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-QnAシリーズ アクセス局2",
                SupportedDevices = GetQnADevices()
            },

            // Aシリーズ
            MitsubishiPLCSeries.AJ71E71 => new()
            {
                Series = series,
                DefaultPort = 5040,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-Aシリーズ",
                SupportedDevices = GetASeriesDevices()
            },

            // FXシリーズ
            MitsubishiPLCSeries.FX3U_ENET => new()
            {
                Series = series,
                DefaultPort = 5050,
                IsBinaryProtocol = true,
                NetworkNumber = 0x00,
                StationNumber = 0xFF,
                ModuleIONumber = 0x03FF,
                MultiDropStationNumber = 0x00,
                Description = "MELSEC-FXシリーズ FX3U-ENET",
                SupportedDevices = GetFXDevices()
            },

            _ => throw new NotSupportedException($"未対応のPLCシリーズです: {series}")
        };
    }

    // Q/L/iQ-Rシリーズ対応デバイス
    private static Dictionary<string, (byte Code, bool IsWordDevice)> GetQLiQRDevices()
    {
        return new Dictionary<string, (byte Code, bool IsWordDevice)>
        {
            { "X", (0x9C, false) },    // 入力リレー
            { "Y", (0x9D, false) },    // 出力リレー
            { "M", (0x90, false) },    // 内部リレー
            { "L", (0x92, false) },    // ラッチリレー
            { "F", (0x93, false) },    // アナンシエータ
            { "V", (0x94, false) },    // エッジリレー
            { "B", (0xA0, false) },    // リンクリレー
            { "D", (0xA8, true) },     // データレジスタ
            { "W", (0xB4, true) },     // リンクレジスタ
            { "TS", (0xC1, false) },   // タイマ接点
            { "TC", (0xC0, false) },   // タイマコイル
            { "TN", (0xC2, true) },    // タイマ現在値
            { "CS", (0xC4, false) },   // カウンタ接点
            { "CC", (0xC3, false) },   // カウンタコイル
            { "CN", (0xC5, true) },    // カウンタ現在値
            { "SB", (0xA1, false) },   // リンク特殊リレー
            { "SW", (0xB5, true) },    // リンク特殊レジスタ
            { "S", (0x98, false) },    // ステップリレー
            { "DX", (0xA2, false) },   // 直接入力
            { "DY", (0xA3, false) },   // 直接出力
            { "Z", (0xCC, true) },     // インデックスレジスタ
            { "R", (0xAF, true) },     // ファイルレジスタ
            { "ZR", (0xB0, true) }     // ファイルレジスタ（拡張）
        };
    }

    // iQ-Fシリーズ FX5U対応デバイス
    private static Dictionary<string, (byte Code, bool IsWordDevice)> GetFX5UDevices()
    {
        return new Dictionary<string, (byte Code, bool IsWordDevice)>
        {
            { "X", (0x9C, false) },    // 入力リレー
            { "Y", (0x9D, false) },    // 出力リレー
            { "M", (0x90, false) },    // 補助リレー
            { "L", (0x92, false) },    // ラッチリレー
            { "B", (0xA0, false) },    // リンクリレー
            { "D", (0xA8, true) },     // データレジスタ
            { "W", (0xB4, true) },     // リンクレジスタ
            { "TS", (0xC1, false) },   // タイマ接点
            { "TC", (0xC0, false) },   // タイマコイル
            { "TN", (0xC2, true) },    // タイマ現在値
            { "CS", (0xC4, false) },   // カウンタ接点
            { "CC", (0xC3, false) },   // カウンタコイル
            { "CN", (0xC5, true) },    // カウンタ現在値
            { "SM", (0x91, false) },   // 特殊補助リレー
            { "SD", (0xA9, true) },    // 特殊データレジスタ
            { "S", (0x98, false) },    // ステップリレー
            { "Z", (0xCC, true) }      // インデックスレジスタ
        };
    }

    // QnAシリーズ対応デバイス
    private static Dictionary<string, (byte Code, bool IsWordDevice)> GetQnADevices()
    {
        return new Dictionary<string, (byte Code, bool IsWordDevice)>
        {
            { "X", (0x9C, false) },    // 入力リレー
            { "Y", (0x9D, false) },    // 出力リレー
            { "M", (0x90, false) },    // 内部リレー
            { "L", (0x92, false) },    // ラッチリレー
            { "F", (0x93, false) },    // アナンシエータ
            { "V", (0x94, false) },    // エッジリレー
            { "B", (0xA0, false) },    // リンクリレー
            { "D", (0xA8, true) },     // データレジスタ
            { "W", (0xB4, true) },     // リンクレジスタ
            { "TS", (0xC1, false) },   // タイマ接点
            { "TC", (0xC0, false) },   // タイマコイル
            { "TN", (0xC2, true) },    // タイマ現在値
            { "CS", (0xC4, false) },   // カウンタ接点
            { "CC", (0xC3, false) },   // カウンタコイル
            { "CN", (0xC5, true) },    // カウンタ現在値
            { "SB", (0xA1, false) },   // リンク特殊リレー
            { "SW", (0xB5, true) },    // リンク特殊レジスタ
            { "S", (0x98, false) },    // ステップリレー
            { "Z", (0xCC, true) },     // インデックスレジスタ
            { "R", (0xAF, true) }      // ファイルレジスタ
        };
    }

    // Aシリーズ対応デバイス
    private static Dictionary<string, (byte Code, bool IsWordDevice)> GetASeriesDevices()
    {
        return new Dictionary<string, (byte Code, bool IsWordDevice)>
        {
            { "X", (0x9C, false) },    // 入力リレー
            { "Y", (0x9D, false) },    // 出力リレー
            { "M", (0x90, false) },    // 内部リレー
            { "L", (0x92, false) },    // ラッチリレー
            { "F", (0x93, false) },    // アナンシエータ
            { "B", (0xA0, false) },    // リンクリレー
            { "D", (0xA8, true) },     // データレジスタ
            { "W", (0xB4, true) },     // リンクレジスタ
            { "TS", (0xC1, false) },   // タイマ接点
            { "TC", (0xC0, false) },   // タイマコイル
            { "TN", (0xC2, true) },    // タイマ現在値
            { "CS", (0xC4, false) },   // カウンタ接点
            { "CC", (0xC3, false) },   // カウンタコイル
            { "CN", (0xC5, true) },    // カウンタ現在値
            { "SB", (0xA1, false) },   // リンク特殊リレー
            { "SW", (0xB5, true) },    // リンク特殊レジスタ
            { "S", (0x98, false) },    // ステップリレー
            { "Z", (0xCC, true) },     // インデックスレジスタ
            { "R", (0xAF, true) }      // ファイルレジスタ
        };
    }

    // FXシリーズ対応デバイス
    private static Dictionary<string, (byte Code, bool IsWordDevice)> GetFXDevices()
    {
        return new Dictionary<string, (byte Code, bool IsWordDevice)>
        {
            { "X", (0x9C, false) },    // 入力リレー
            { "Y", (0x9D, false) },    // 出力リレー
            { "M", (0x90, false) },    // 補助リレー
            { "D", (0xA8, true) },     // データレジスタ
            { "TS", (0xC1, false) },   // タイマ接点
            { "TC", (0xC0, false) },   // タイマコイル
            { "TN", (0xC2, true) },    // タイマ現在値
            { "CS", (0xC4, false) },   // カウンタ接点
            { "CC", (0xC3, false) },   // カウンタコイル
            { "CN", (0xC5, true) },    // カウンタ現在値
            { "S", (0x98, false) }     // ステップリレー
        };
    }
}