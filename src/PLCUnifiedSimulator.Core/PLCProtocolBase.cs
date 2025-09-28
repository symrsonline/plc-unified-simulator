namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLC通信の基底クラス
/// </summary>
public abstract class PLCProtocolBase : IPLCProtocol, IDisposable
{
    protected bool _isConnected = false;
    protected bool _disposed = false;

    public abstract string ProtocolName { get; }
    public abstract int DefaultPort { get; }
    public bool IsConnected => _isConnected;

    public abstract Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync();
    public abstract Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default);
    public abstract Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default);

    public virtual async Task<IEnumerable<PLCData>> ReadMultipleAsync(IEnumerable<PLCAddress> addresses, CancellationToken cancellationToken = default)
    {
        var results = new List<PLCData>();
        foreach (var address in addresses)
        {
            var data = await ReadAsync(address, cancellationToken);
            if (data != null)
            {
                results.Add(data);
            }
        }
        return results;
    }

    public virtual async Task<bool> WriteMultipleAsync(IEnumerable<(PLCAddress Address, byte[] Data)> data, CancellationToken cancellationToken = default)
    {
        foreach (var (address, bytes) in data)
        {
            if (!await WriteAsync(address, bytes, cancellationToken))
            {
                return false;
            }
        }
        return true;
    }

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}