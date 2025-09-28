using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// PLCシミュレータの基底クラスを提供します。
/// このクラスはPLC通信プロトコルのシミュレーションに必要な基本的な機能を実装し、
/// 各PLCプロトコル（三菱MC、オムロンFINSなど）のシミュレータ実装基盤となります。
/// </summary>
public abstract class PLCSimulatorBase : IPLCSimulator, IDisposable
{
    protected readonly ConcurrentDictionary<string, byte[]> _deviceMemory = new();
    protected TcpListener? _tcpListener;
    protected UdpClient? _udpListener;
    protected bool _isRunning = false;
    protected bool _isTcpRunning = false;
    protected bool _isUdpRunning = false;
    protected CancellationTokenSource? _cancellationTokenSource;
    protected readonly object _lockObject = new();
    protected readonly ILogger _logger;
    private bool _disposed = false;

    /// <summary>
    /// 使用するPLC通信プロトコルを取得します
    /// </summary>
    public abstract IPLCProtocol Protocol { get; }

    /// <summary>
    /// ロガーインスタンスを取得します
    /// </summary>
    public ILogger Logger => _logger;

    /// <summary>
    /// シミュレータの実行状態を取得します
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// PLCSimulatorBaseクラスの新しいインスタンスを初期化します
    /// </summary>
    /// <param name="logger">ログ出力に使用するロガーインスタンス。nullの場合はNullLoggerを使用します。</param>
    protected PLCSimulatorBase(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger<PLCSimulatorBase>.Instance;
    }

    /// <summary>
    /// TCPサーバーを開始し、PLCクライアントからの接続を待機します
    /// </summary>
    /// <param name="port">TCPサーバーがリッスンするポート番号</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>サーバー開始処理の完了を表すTask</returns>
    /// <exception cref="Exception">サーバー開始に失敗した場合</exception>
    public virtual async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("TCPシミュレータは既に実行中です");
            return;
        }

        try
        {
            _logger.LogInformation("TCPシミュレータの開始を開始します: ポート {Port}", port);
            _cancellationTokenSource = new CancellationTokenSource();
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            _isRunning = true;

            _logger.LogInformation("{ProtocolName} TCPシミュレータがポート {Port} で開始されました", Protocol.ProtocolName, port);

            // クライアント接続を待機
            _ = Task.Run(async () => await AcceptClientsAsync(_cancellationTokenSource.Token), cancellationToken);
            _isTcpRunning = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCPシミュレータの開始に失敗しました: ポート {Port}", port);
            await StopAsync();
        }
    }

    /// <summary>
    /// UDPサーバーを開始し、PLCクライアントからのUDPパケットを待機します
    /// </summary>
    /// <param name="port">UDPサーバーがリッスンするポート番号</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>サーバー開始処理の完了を表すTask</returns>
    /// <exception cref="Exception">サーバー開始に失敗した場合</exception>
    public virtual async Task StartUdpAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_isUdpRunning)
        {
            _logger.LogWarning("UDPシミュレータは既に実行中です");
            return;
        }

        try
        {
            _logger.LogInformation("UDPシミュレータの開始を開始します: ポート {Port}", port);
            _cancellationTokenSource ??= new CancellationTokenSource();
            _udpListener = new UdpClient(port);
            _isUdpRunning = true;
            _isRunning = true;

            _logger.LogInformation("{ProtocolName} UDPシミュレータがポート {Port} で開始されました", Protocol.ProtocolName, port);

            // UDP クライアント接続を待機
            _ = Task.Run(async () => await AcceptUdpClientsAsync(_cancellationTokenSource.Token), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDPシミュレータの開始に失敗しました: ポート {Port}", port);
            await StopAsync();
        }
    }

    /// <summary>
    /// TCPとUDPの両方のサーバーを開始します
    /// </summary>
    /// <param name="tcpPort">TCPサーバーがリッスンするポート番号</param>
    /// <param name="udpPort">UDPサーバーがリッスンするポート番号</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>サーバー開始処理の完了を表すTask</returns>
    public virtual async Task StartBothAsync(int tcpPort, int udpPort, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TCP/UDP両方のシミュレータを開始します: TCPポート {TcpPort}, UDPポート {UdpPort}", tcpPort, udpPort);
        await StartAsync(tcpPort, cancellationToken);
        await StartUdpAsync(udpPort, cancellationToken);
        _logger.LogInformation("TCP/UDP両方のシミュレータが開始されました");
    }

    /// <summary>
    /// シミュレータを停止します
    /// </summary>
    /// <returns>停止処理の完了を表すTask</returns>
    public virtual async Task StopAsync()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("シミュレータは既に停止しています");
            return;
        }

        _logger.LogInformation("シミュレータの停止を開始します");
        lock (_lockObject)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _tcpListener?.Stop();
            _udpListener?.Close();
        }

        _logger.LogInformation("{ProtocolName} シミュレータが停止されました", Protocol.ProtocolName);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 指定されたPLCアドレスにデバイス値を設定します
    /// </summary>
    /// <param name="address">値を設定するPLCアドレス</param>
    /// <param name="value">設定するバイナリ値</param>
    /// <exception cref="ArgumentNullException">valueがnullの場合</exception>
    public virtual void SetDeviceValue(PLCAddress address, byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value), "デバイス値にnullは設定できません");
        }

        var key = $"{address.DeviceType}{address.Address}";
        _deviceMemory[key] = value;
        _logger.LogInformation("デバイス {DeviceKey} に値 {Value} を設定しました", key, BitConverter.ToString(value));
    }

    /// <summary>
    /// 指定されたPLCアドレスからデバイス値を取得します
    /// </summary>
    /// <param name="address">値を取得するPLCアドレス</param>
    /// <returns>デバイス値のバイナリデータ。値が存在しない場合はnull</returns>
    public virtual byte[]? GetDeviceValue(PLCAddress address)
    {
        var key = $"{address.DeviceType}{address.Address}";
        if (_deviceMemory.TryGetValue(key, out var value))
        {
            _logger.LogDebug("デバイス {DeviceKey} の値を取得しました: {Value}", key, value != null ? BitConverter.ToString(value) : "null");
            return value;
        }
        _logger.LogDebug("デバイス {DeviceKey} の値が見つかりません", key);
        return null;
    }

    /// <summary>
    /// TCPクライアントからの接続を処理します
    /// </summary>
    /// <param name="client">接続されたTCPクライアント</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>クライアント処理の完了を表すTask</returns>
    protected abstract Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken);

    /// <summary>
    /// UDPパケットを処理します
    /// </summary>
    /// <param name="data">受信したUDPパケットのデータ</param>
    /// <param name="remoteEndPoint">パケットの送信元エンドポイント</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>パケット処理の完了を表すTask</returns>
    protected abstract Task HandleUdpPacketAsync(byte[] data, System.Net.IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("TCPクライアント接続の待機を開始します");
            while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                var remoteEndPoint = tcpClient.Client.RemoteEndPoint;
                _logger.LogInformation("TCPクライアント {RemoteEndPoint} が接続されました", remoteEndPoint);

                // 各クライアントを独立したタスクで処理
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleClientAsync(tcpClient, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "TCPクライアント {RemoteEndPoint} の処理中にエラーが発生しました", remoteEndPoint);
                    }
                    finally
                    {
                        tcpClient?.Close();
                        _logger.LogInformation("TCPクライアント {RemoteEndPoint} が切断されました", remoteEndPoint);
                    }
                }, cancellationToken);
            }
        }
        catch (ObjectDisposedException)
        {
            // リスナーが停止されたときの正常な終了
            _logger.LogInformation("TCPクライアント受け入れが停止されました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCPクライアント受け入れ中にエラーが発生しました");
        }
    }

    private async Task AcceptUdpClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("UDPパケット受信の待機を開始します");
            var buffer = new byte[1024];
            while (!cancellationToken.IsCancellationRequested && _udpListener != null)
            {
                var result = await _udpListener.ReceiveAsync();
                _logger.LogDebug("UDPパケットを受信しました: {RemoteEndPoint}, サイズ: {Size} bytes", result.RemoteEndPoint, result.Buffer.Length);

                // UDP パケットを独立したタスクで処理
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await HandleUdpPacketAsync(result.Buffer, result.RemoteEndPoint, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "UDPパケット {RemoteEndPoint} の処理中にエラーが発生しました", result.RemoteEndPoint);
                    }
                }, cancellationToken);
            }
        }
        catch (ObjectDisposedException)
        {
            // リスナーが停止されたときの正常な終了
            _logger.LogInformation("UDPパケット受け入れが停止されました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDPパケット受け入れ中にエラーが発生しました");
        }
    }

    /// <summary>
    /// エラーコードに基づいてエラー応答を作成します
    /// </summary>
    /// <param name="errorCode">エラーコード</param>
    /// <returns>エラー応答のバイナリデータ</returns>
    /// <remarks>
    /// このメソッドは基本的な実装を提供します。派生クラスでプロトコル固有のエラー応答を
    /// 作成する場合は、このメソッドをオーバーライドしてください。
    /// </remarks>
    protected byte[] CreateErrorResponse(ushort errorCode)
    {
        // 基本的なエラー応答フレーム（プロトコル固有でオーバーライド）
        return BitConverter.GetBytes(errorCode);
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
                StopAsync().Wait();
                _cancellationTokenSource?.Dispose();
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