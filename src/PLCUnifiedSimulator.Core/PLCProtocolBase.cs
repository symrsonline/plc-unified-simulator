using Microsoft.Extensions.Logging;

namespace PLCUnifiedSimulator.Core;

/// <summary>
/// PLC通信の基底クラス
/// </summary>
public abstract class PLCProtocolBase : IPLCProtocol, IDisposable
{
    protected bool _isConnected = false;
    protected bool _disposed = false;
    protected readonly ILogger _logger;

    public abstract string ProtocolName { get; }
    public abstract int DefaultPort { get; }
    public ILogger Logger => _logger;
    public bool IsConnected => _isConnected;

    protected PLCProtocolBase(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public abstract Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default);
    public abstract Task<bool> ConnectUdpAsync(string ipAddress, int port, CancellationToken cancellationToken = default);
    public abstract Task DisconnectAsync();
    public abstract Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default);
    public abstract Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default);

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