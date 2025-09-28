namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLCデバイスアドレスを表します
/// </summary>
public class PLCAddress
{
    public string DeviceType { get; set; } = string.Empty;
    public int Address { get; set; }
    public int Size { get; set; }
    
    public PLCAddress(string deviceType, int address, int size = 1)
    {
        DeviceType = deviceType;
        Address = address;
        Size = size;
    }

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
    public PLCAddress Address { get; set; }
    public byte[] Data { get; set; }
    public DateTime Timestamp { get; set; }

    public PLCData(PLCAddress address, byte[] data)
    {
        Address = address;
        Data = data;
        Timestamp = DateTime.Now;
    }

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