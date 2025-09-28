using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// PLCシミュレータの基底クラス
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

    public abstract IPLCProtocol Protocol { get; }
    public ILogger Logger => _logger;
    public bool IsRunning => _isRunning;

    protected PLCSimulatorBase(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger<PLCSimulatorBase>.Instance;
    }

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

    public virtual async Task StartBothAsync(int tcpPort, int udpPort, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TCP/UDP両方のシミュレータを開始します: TCPポート {TcpPort}, UDPポート {UdpPort}", tcpPort, udpPort);
        await StartAsync(tcpPort, cancellationToken);
        await StartUdpAsync(udpPort, cancellationToken);
        _logger.LogInformation("TCP/UDP両方のシミュレータが開始されました");
    }

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

    protected abstract Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken);

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

    protected abstract Task HandleUdpPacketAsync(byte[] data, System.Net.IPEndPoint remoteEndPoint, CancellationToken cancellationToken);

    protected byte[] CreateErrorResponse(ushort errorCode)
    {
        // 基本的なエラー応答フレーム（プロトコル固有でオーバーライド）
        return BitConverter.GetBytes(errorCode);
    }

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}