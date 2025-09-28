namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLC通信のインターフェース
/// </summary>
public interface IPLCProtocol
{
    /// <summary>
    /// プロトコル名
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// デフォルトポート番号
    /// </summary>
    int DefaultPort { get; }

    /// <summary>
    /// 接続を開始します
    /// </summary>
    Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// 接続を切断します
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// PLCからデータを読み取ります
    /// </summary>
    Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// PLCにデータを書き込みます
    /// </summary>
    Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 複数のデバイスからデータを読み取ります
    /// </summary>
    Task<IEnumerable<PLCData>> ReadMultipleAsync(IEnumerable<PLCAddress> addresses, CancellationToken cancellationToken = default);

    /// <summary>
    /// 複数のデバイスにデータを書き込みます
    /// </summary>
    Task<bool> WriteMultipleAsync(IEnumerable<(PLCAddress Address, byte[] Data)> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 接続状態を取得します
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// PLCシミュレータのインターフェース
/// </summary>
public interface IPLCSimulator
{
    /// <summary>
    /// サポートしているプロトコル
    /// </summary>
    IPLCProtocol Protocol { get; }

    /// <summary>
    /// シミュレータを開始します
    /// </summary>
    Task StartAsync(int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// シミュレータを停止します
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// デバイス値を設定します（シミュレーション用）
    /// </summary>
    void SetDeviceValue(PLCAddress address, byte[] value);

    /// <summary>
    /// デバイス値を取得します（シミュレーション用）
    /// </summary>
    byte[]? GetDeviceValue(PLCAddress address);

    /// <summary>
    /// シミュレータの実行状態
    /// </summary>
    bool IsRunning { get; }
}