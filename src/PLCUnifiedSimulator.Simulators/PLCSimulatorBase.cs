using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// PLCシミュレータの基底クラス
/// </summary>
public abstract class PLCSimulatorBase : IPLCSimulator, IDisposable
{
    protected readonly ConcurrentDictionary<string, byte[]> _deviceMemory = new();
    protected TcpListener? _tcpListener;
    protected bool _isRunning = false;
    protected CancellationTokenSource? _cancellationTokenSource;
    protected readonly object _lockObject = new();
    private bool _disposed = false;

    public abstract IPLCProtocol Protocol { get; }
    public bool IsRunning => _isRunning;

    public virtual async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _tcpListener.Start();
            _isRunning = true;

            Console.WriteLine($"{Protocol.ProtocolName} シミュレータがポート {port} で開始されました。");

            // クライアント接続を待機
            _ = Task.Run(async () => await AcceptClientsAsync(_cancellationTokenSource.Token), cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"シミュレータの開始に失敗しました: {ex.Message}");
            await StopAsync();
        }
    }

    public virtual async Task StopAsync()
    {
        if (!_isRunning) return;

        lock (_lockObject)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _tcpListener?.Stop();
        }

        Console.WriteLine($"{Protocol.ProtocolName} シミュレータが停止されました。");
    }

    public virtual void SetDeviceValue(PLCAddress address, byte[] value)
    {
        var key = $"{address.DeviceType}{address.Address}";
        _deviceMemory[key] = value;
        Console.WriteLine($"デバイス {key} に値 {BitConverter.ToString(value)} を設定しました。");
    }

    public virtual byte[]? GetDeviceValue(PLCAddress address)
    {
        var key = $"{address.DeviceType}{address.Address}";
        _deviceMemory.TryGetValue(key, out var value);
        return value;
    }

    protected abstract Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken);

    private async Task AcceptClientsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _tcpListener != null)
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                Console.WriteLine($"クライアント {tcpClient.Client.RemoteEndPoint} が接続されました。");

                // 各クライアントを独立したタスクで処理
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await HandleClientAsync(tcpClient, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"クライアント処理エラー: {ex.Message}");
                    }
                    finally
                    {
                        tcpClient?.Close();
                        Console.WriteLine($"クライアント {tcpClient?.Client.RemoteEndPoint} が切断されました。");
                    }
                }, cancellationToken);
            }
        }
        catch (ObjectDisposedException)
        {
            // リスナーが停止されたときの正常な終了
        }
        catch (Exception ex)
        {
            Console.WriteLine($"クライアント受け入れエラー: {ex.Message}");
        }
    }

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