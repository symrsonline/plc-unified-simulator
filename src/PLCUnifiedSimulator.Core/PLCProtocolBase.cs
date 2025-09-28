using Microsoft.Extensions.Logging;

namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLC通信プロトコルの基底クラスを提供します。
/// このクラスはPLCとの通信に必要な基本的な機能を実装し、
/// 各PLCプロトコル（三菱MC、オムロンFINSなど）の実装基盤となります。
/// </summary>
public abstract class PLCProtocolBase : IPLCProtocol, IDisposable
{
    protected bool _isConnected = false;
    protected bool _disposed = false;
    protected readonly ILogger _logger;

    /// <summary>
    /// プロトコル名を取得します
    /// </summary>
    public abstract string ProtocolName { get; }

    /// <summary>
    /// デフォルトのポート番号を取得します
    /// </summary>
    public abstract int DefaultPort { get; }

    /// <summary>
    /// ロガーインスタンスを取得します
    /// </summary>
    public ILogger Logger => _logger;

    /// <summary>
    /// PLCとの接続状態を取得します
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// PLCProtocolBaseクラスの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="logger">ログ出力に使用するロガーインスタンス</param>
    /// <exception cref="ArgumentNullException">loggerがnullの場合</exception>
    protected PLCProtocolBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// PLCにTCP接続します
    /// </summary>
    /// <param name="ipAddress">PLCのIPアドレス</param>
    /// <param name="port">接続ポート番号</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>接続成功の場合はtrue、失敗の場合はfalse</returns>
    public abstract Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// PLCにUDP接続します
    /// </summary>
    /// <param name="ipAddress">PLCのIPアドレス</param>
    /// <param name="port">接続ポート番号</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>接続成功の場合はtrue、失敗の場合はfalse</returns>
    public abstract Task<bool> ConnectUdpAsync(string ipAddress, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// PLCとの接続を切断します
    /// </summary>
    /// <returns>切断処理の完了を表すTask</returns>
    public abstract Task DisconnectAsync();

    /// <summary>
    /// PLCからデータを読み取ります
    /// </summary>
    /// <param name="address">読み取り対象のPLCアドレス</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>読み取り成功時はPLCDataオブジェクト、失敗時はnull</returns>
    public abstract Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default);

    /// <summary>
    /// PLCにデータを書き込みます
    /// </summary>
    /// <param name="address">書き込み対象のPLCアドレス</param>
    /// <param name="data">書き込むバイナリデータ</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>書き込み成功の場合はtrue、失敗の場合はfalse</returns>
    public abstract Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 複数のPLCアドレスからデータを一括で読み取ります
    /// </summary>
    /// <param name="addresses">読み取り対象のPLCアドレスのコレクション</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>読み取りに成功したPLCDataオブジェクトのコレクション</returns>
    /// <remarks>
    /// このメソッドはデフォルト実装として、各アドレスを個別に読み取る処理を行います。
    /// 派生クラスでプロトコル固有の一括読み取り機能をサポートしている場合は、
    /// このメソッドをオーバーライドすることを推奨します。
    /// </remarks>
    public virtual async Task<IEnumerable<PLCData>> ReadMultipleAsync(IEnumerable<PLCAddress> addresses, CancellationToken cancellationToken = default)
    {
        var results = new List<PLCData>();
        foreach (var address in addresses)
        {
            _logger.LogDebug("複数読み取り: {DeviceType}{Address} (サイズ: {Size})", address.DeviceType, address.Address, address.Size);
            var data = await ReadAsync(address, cancellationToken);
            if (data != null)
            {
                results.Add(data);
                _logger.LogDebug("複数読み取り成功: {DeviceType}{Address}", address.DeviceType, address.Address);
            }
            else
            {
                _logger.LogWarning("複数読み取り失敗: {DeviceType}{Address}", address.DeviceType, address.Address);
            }
        }
        _logger.LogInformation("複数読み取り完了: {SuccessCount}/{TotalCount} 件成功", results.Count, addresses.Count());
        return results;
    }

    /// <summary>
    /// 複数のPLCアドレスにデータを一括で書き込みます
    /// </summary>
    /// <param name="data">書き込み対象のアドレスとデータのペアのコレクション</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>すべての書き込みが成功した場合はtrue、一部または全部が失敗した場合はfalse</returns>
    /// <remarks>
    /// このメソッドはデフォルト実装として、各アドレスに個別に書き込む処理を行います。
    /// 派生クラスでプロトコル固有の一括書き込み機能をサポートしている場合は、
    /// このメソッドをオーバーライドすることを推奨します。
    /// </remarks>
    public virtual async Task<bool> WriteMultipleAsync(IEnumerable<(PLCAddress Address, byte[] Data)> data, CancellationToken cancellationToken = default)
    {
        var totalCount = data.Count();
        var successCount = 0;

        foreach (var (address, bytes) in data)
        {
            _logger.LogDebug("複数書き込み: {DeviceType}{Address}, データサイズ: {DataSize} bytes", address.DeviceType, address.Address, bytes.Length);
            if (await WriteAsync(address, bytes, cancellationToken))
            {
                successCount++;
                _logger.LogDebug("複数書き込み成功: {DeviceType}{Address}", address.DeviceType, address.Address);
            }
            else
            {
                _logger.LogWarning("複数書き込み失敗: {DeviceType}{Address}", address.DeviceType, address.Address);
            }
        }

        var isAllSuccess = successCount == totalCount;
        _logger.LogInformation("複数書き込み完了: {SuccessCount}/{TotalCount} 件成功", successCount, totalCount);
        return isAllSuccess;
    }

    /// <summary>
    /// リソースの解放を行います
    /// </summary>
    /// <param name="disposing">マネージドリソースも解放する場合はtrue</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DisconnectAsync().Wait();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// リソースを解放します
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}