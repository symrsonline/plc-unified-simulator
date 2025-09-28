namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLCデバイスアドレスを表します
/// </summary>
public class PLCAddress
{
    /// <summary>
    /// デバイスタイプ（例: "D", "M", "X", "Y"）
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// デバイスアドレス番号
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// アクセスサイズ（ワード単位）
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// PLCAddressクラスの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="deviceType">デバイスタイプ</param>
    /// <param name="address">デバイスアドレス番号</param>
    /// <param name="size">アクセスサイズ（デフォルト: 1）</param>
    public PLCAddress(string deviceType, int address, int size = 1)
    {
        DeviceType = deviceType;
        Address = address;
        Size = size;
    }

    /// <summary>
    /// アドレスを文字列形式で返します
    /// </summary>
    /// <returns>デバイスタイプとアドレスを組み合わせた文字列（例: "D100"）</returns>
    public override string ToString()
    {
        return $"{DeviceType}{Address}";
    }
}

/// <summary>
/// PLCデータを表します
/// </summary>
public class PLCData
{
    /// <summary>
    /// PLCアドレス情報
    /// </summary>
    public PLCAddress Address { get; set; }

    /// <summary>
    /// バイナリデータ
    /// </summary>
    public byte[] Data { get; set; }

    /// <summary>
    /// データ取得時のタイムスタンプ
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// PLCDataクラスの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="address">PLCアドレス情報</param>
    /// <param name="data">バイナリデータ</param>
    public PLCData(PLCAddress address, byte[] data)
    {
        Address = address;
        Data = data;
        Timestamp = DateTime.Now;
    }

    /// <summary>
    /// バイナリデータを指定された型に変換して返します
    /// </summary>
    /// <typeparam name="T">変換先の型（short, int, float, bool）</typeparam>
    /// <returns>変換された値</returns>
    /// <exception cref="NotSupportedException">サポートされていない型が指定された場合</exception>
    /// <exception cref="ArgumentException">データサイズが不十分な場合</exception>
    public T GetValue<T>() where T : struct
    {
        return typeof(T) switch
        {
            var t when t == typeof(short) => (T)(object)BitConverter.ToInt16(Data, 0),
            var t when t == typeof(int) => (T)(object)BitConverter.ToInt32(Data, 0),
            var t when t == typeof(float) => (T)(object)BitConverter.ToSingle(Data, 0),
            var t when t == typeof(bool) => (T)(object)(Data[0] != 0),
            _ => throw new NotSupportedException($"Type {typeof(T)} is not supported")
        };
    }
}