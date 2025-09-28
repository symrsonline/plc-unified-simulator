using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Protocols.Mitsubishi;

/// <summary>
/// 三菱MCプロトコル（全シリーズ対応）の実装
/// </summary>
public class MitsubishiMCProtocol : PLCProtocolBase
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly object _lockObject = new();
    private MitsubishiPLCSeriesInfo _seriesInfo;

    public MitsubishiPLCSeries PLCSeries { get; }
    public override string ProtocolName => $"Mitsubishi MC Protocol ({_seriesInfo.Description})";
    public override int DefaultPort => _seriesInfo.DefaultPort;

    public MitsubishiMCProtocol(MitsubishiPLCSeries series = MitsubishiPLCSeries.QJ71E71_Binary_Station1, ILogger? logger = null)
        : base(logger ?? NullLogger<MitsubishiMCProtocol>.Instance)
    {
        PLCSeries = series;
        _seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
    }

    public override async Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("TCP接続を開始します: {IPAddress}:{Port}", ipAddress, port);
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port, cancellationToken);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            _logger.LogInformation("TCP接続に成功しました: {IPAddress}:{Port}", ipAddress, port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP接続に失敗しました: {IPAddress}:{Port}", ipAddress, port);
            await DisconnectAsync();
            return false;
        }
    }

    public override async Task<bool> ConnectUdpAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        // UDPは接続レス型プロトコルのため、常に成功とする
        _logger.LogInformation("UDP接続を設定します: {IPAddress}:{Port}", ipAddress, port);
        _isConnected = true;
        _logger.LogInformation("UDP接続設定が完了しました: {IPAddress}:{Port}", ipAddress, port);
        await Task.CompletedTask;
        return true;
    }

    public override async Task DisconnectAsync()
    {
        _logger.LogInformation("接続を切断します");
        lock (_lockObject)
        {
            _stream?.Close();
            _stream?.Dispose();
            _tcpClient?.Close();
            _tcpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
            _isConnected = false;
        }
        _logger.LogInformation("接続が切断されました");
        await Task.CompletedTask;
    }

    public override async Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _stream == null)
        {
            _logger.LogWarning("読み取り要求が拒否されました: 接続されていません");
            return null;
        }

        try
        {
            _logger.LogDebug("デバイス読み取りを開始します: {DeviceType}{Address} (サイズ: {Size})", address.DeviceType, address.Address, address.Size);

            // デバイスアクセス前の検証
            ValidateDeviceAccess(address);

            var request = CreateReadRequest(address);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[1024];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);

            if (IsValidResponse(response, bytesRead))
            {
                var data = ExtractDataFromResponse(response, bytesRead, address.Size * 2); // 2 bytes per word
                _logger.LogDebug("デバイス読み取りに成功しました: {DeviceType}{Address}, データサイズ: {DataSize} bytes", address.DeviceType, address.Address, data.Length);
                return new PLCData(address, data);
            }

            _logger.LogWarning("デバイス読み取りに失敗しました: 無効なレスポンスを受信しました - {DeviceType}{Address}", address.DeviceType, address.Address);
            return null;
        }
        catch (NotSupportedException)
        {
            // 未サポートデバイスの例外は再スロー
            _logger.LogError("未サポートデバイスへのアクセスが試行されました: {DeviceType}", address.DeviceType);
            throw;
        }
        catch (ArgumentException)
        {
            // 引数エラーの例外は再スロー
            _logger.LogError("無効なアドレス指定です: {DeviceType}{Address}", address.DeviceType, address.Address);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "デバイス読み取り中にエラーが発生しました: {DeviceType}{Address}", address.DeviceType, address.Address);
            return null;
        }
    }

    public override async Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _stream == null)
        {
            _logger.LogWarning("書き込み要求が拒否されました: 接続されていません");
            return false;
        }

        try
        {
            _logger.LogDebug("デバイス書き込みを開始します: {DeviceType}{Address}, データサイズ: {DataSize} bytes", address.DeviceType, address.Address, data.Length);

            // デバイスアクセス前の検証
            ValidateDeviceAccess(address);

            var request = CreateWriteRequest(address, data);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[256];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);

            var success = IsValidResponse(response, bytesRead);
            if (success)
            {
                _logger.LogDebug("デバイス書き込みに成功しました: {DeviceType}{Address}", address.DeviceType, address.Address);
            }
            else
            {
                _logger.LogWarning("デバイス書き込みに失敗しました: 無効なレスポンスを受信しました - {DeviceType}{Address}", address.DeviceType, address.Address);
            }
            return success;
        }
        catch (NotSupportedException)
        {
            // 未サポートデバイスの例外は再スロー
            _logger.LogError("未サポートデバイスへのアクセスが試行されました: {DeviceType}", address.DeviceType);
            throw;
        }
        catch (ArgumentException)
        {
            // 引数エラーの例外は再スロー
            _logger.LogError("無効なアドレス指定です: {DeviceType}{Address}", address.DeviceType, address.Address);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "デバイス書き込み中にエラーが発生しました: {DeviceType}{Address}", address.DeviceType, address.Address);
            return false;
        }
    }

    private byte[] CreateReadRequest(PLCAddress address)
    {
        if (_seriesInfo.IsBinaryProtocol)
        {
            return CreateBinaryReadRequest(address);
        }
        else
        {
            return CreateASCIIReadRequest(address);
        }
    }

    private byte[] CreateBinaryReadRequest(PLCAddress address)
    {
        // MCプロトコル バイナリ バッチ読み出し要求フレーム
        var frame = new List<byte>();

        // フレームヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.Add(_seriesInfo.NetworkNumber); // 要求先ネットワーク番号
        frame.Add(_seriesInfo.StationNumber); // 要求先局番号
        frame.AddRange(BitConverter.GetBytes(_seriesInfo.ModuleIONumber)); // 要求先ユニットI/O番号
        frame.Add(_seriesInfo.MultiDropStationNumber); // 要求先マルチドロップ局番号
        frame.AddRange(BitConverter.GetBytes((ushort)18)); // 要求データ長

        // コマンド
        frame.AddRange(BitConverter.GetBytes((ushort)0x0401)); // バッチ読み出し

        // サブコマンド
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));

        // デバイスコードとアドレス
        var deviceInfo = GetDeviceInfo(address.DeviceType);
        frame.AddRange(BitConverter.GetBytes(address.Address)); // 先頭デバイス番号(3バイト)
        frame.Add(0x00);
        frame.Add(deviceInfo.Code); // デバイスコード
        frame.AddRange(BitConverter.GetBytes((ushort)address.Size)); // デバイス点数

        return frame.ToArray();
    }

    private byte[] CreateASCIIReadRequest(PLCAddress address)
    {
        // MCプロトコル ASCII バッチ読み出し要求フレーム
        var deviceInfo = GetDeviceInfo(address.DeviceType);

        var command = $"0401" + // コマンド
                     $"0000" + // サブコマンド
                     $"{address.Address:D6}" + // デバイス番号（6桁）
                     $"{address.DeviceType}" + // デバイスコード
                     $"{address.Size:D4}"; // 点数（4桁）

        var frame = new List<byte>();
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.NetworkNumber:X2}")); // ネットワーク番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.StationNumber:X2}")); // 局番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.ModuleIONumber:X4}")); // モジュールI/O番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.MultiDropStationNumber:X2}")); // マルチドロップ局番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{command.Length:X4}")); // データ長
        frame.AddRange(Encoding.ASCII.GetBytes(command)); // コマンドデータ

        return frame.ToArray();
    }

    private byte[] CreateWriteRequest(PLCAddress address, byte[] data)
    {
        if (_seriesInfo.IsBinaryProtocol)
        {
            return CreateBinaryWriteRequest(address, data);
        }
        else
        {
            return CreateASCIIWriteRequest(address, data);
        }
    }

    private byte[] CreateBinaryWriteRequest(PLCAddress address, byte[] data)
    {
        // MCプロトコル バイナリ バッチ書き込み要求フレーム
        var frame = new List<byte>();

        // フレームヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.Add(_seriesInfo.NetworkNumber); // 要求先ネットワーク番号
        frame.Add(_seriesInfo.StationNumber); // 要求先局番号
        frame.AddRange(BitConverter.GetBytes(_seriesInfo.ModuleIONumber)); // 要求先ユニットI/O番号
        frame.Add(_seriesInfo.MultiDropStationNumber); // 要求先マルチドロップ局番号
        frame.AddRange(BitConverter.GetBytes((ushort)(18 + data.Length))); // 要求データ長

        // コマンド
        frame.AddRange(BitConverter.GetBytes((ushort)0x1401)); // バッチ書き込み

        // サブコマンド
        frame.AddRange(BitConverter.GetBytes((ushort)0x0000));

        // デバイスコードとアドレス
        var deviceInfo = GetDeviceInfo(address.DeviceType);
        frame.AddRange(BitConverter.GetBytes(address.Address)); // 先頭デバイス番号(3バイト)
        frame.Add(0x00);
        frame.Add(deviceInfo.Code); // デバイスコード
        frame.AddRange(BitConverter.GetBytes((ushort)address.Size)); // デバイス点数

        // データ
        frame.AddRange(data);

        return frame.ToArray();
    }

    private byte[] CreateASCIIWriteRequest(PLCAddress address, byte[] data)
    {
        // MCプロトコル ASCII バッチ書き込み要求フレーム
        var deviceInfo = GetDeviceInfo(address.DeviceType);

        var dataHex = Convert.ToHexString(data);
        var command = $"1401" + // コマンド
                     $"0000" + // サブコマンド
                     $"{address.Address:D6}" + // デバイス番号（6桁）
                     $"{address.DeviceType}" + // デバイスコード
                     $"{address.Size:D4}" + // 点数（4桁）
                     dataHex; // データ（16進ASCII）

        var frame = new List<byte>();
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.NetworkNumber:X2}")); // ネットワーク番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.StationNumber:X2}")); // 局番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.ModuleIONumber:X4}")); // モジュールI/O番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{_seriesInfo.MultiDropStationNumber:X2}")); // マルチドロップ局番号
        frame.AddRange(Encoding.ASCII.GetBytes($"{command.Length:X4}")); // データ長
        frame.AddRange(Encoding.ASCII.GetBytes(command)); // コマンドデータ

        return frame.ToArray();
    }

    private bool IsValidResponse(byte[] response, int length)
    {
        if (length < 11) return false;

        // エラーコードをチェック (オフセット9-10)
        var errorCode = BitConverter.ToUInt16(response, 9);
        return errorCode == 0;
    }

    private byte[] ExtractDataFromResponse(byte[] response, int length, int dataSize)
    {
        // データ部分は11バイト目から開始
        var data = new byte[dataSize];
        Array.Copy(response, 11, data, 0, Math.Min(dataSize, length - 11));
        return data;
    }

    private (byte Code, string Name) GetDeviceInfo(string deviceType)
    {
        var upperDeviceType = deviceType.ToUpper();

        if (_seriesInfo.SupportedDevices.ContainsKey(upperDeviceType))
        {
            var device = _seriesInfo.SupportedDevices[upperDeviceType];
            return (device.Code, upperDeviceType);
        }

        // サポートされていないデバイスの場合は例外をスロー
        throw new NotSupportedException(
            $"デバイス '{deviceType}' は {_seriesInfo.Description} でサポートされていません。" +
            $"サポートされているデバイス: {string.Join(", ", _seriesInfo.SupportedDevices.Keys)}");
    }

    /// <summary>
    /// 指定されたデバイスタイプがサポートされているかチェック
    /// </summary>
    /// <param name="deviceType">デバイスタイプ</param>
    /// <returns>サポートされている場合はtrue</returns>
    public bool IsDeviceSupported(string deviceType)
    {
        var upperDeviceType = deviceType.ToUpper();
        return _seriesInfo.SupportedDevices.ContainsKey(upperDeviceType);
    }

    /// <summary>
    /// デバイスアクセス前の検証を実行
    /// </summary>
    /// <param name="address">PLCアドレス</param>
    /// <exception cref="NotSupportedException">未サポートデバイスの場合</exception>
    /// <exception cref="ArgumentException">無効なアドレスの場合</exception>
    private void ValidateDeviceAccess(PLCAddress address)
    {
        if (string.IsNullOrEmpty(address.DeviceType))
        {
            throw new ArgumentException("デバイスタイプが指定されていません", nameof(address));
        }

        if (!IsDeviceSupported(address.DeviceType))
        {
            throw new NotSupportedException(
                $"デバイス '{address.DeviceType}' は {_seriesInfo.Description} でサポートされていません。" +
                $"サポートされているデバイス: {string.Join(", ", _seriesInfo.SupportedDevices.Keys)}");
        }

        if (address.Address < 0)
        {
            throw new ArgumentException("デバイスアドレスは0以上である必要があります", nameof(address));
        }

        if (address.Size <= 0)
        {
            throw new ArgumentException("アクセスサイズは1以上である必要があります", nameof(address));
        }
    }

    /// <summary>
    /// 指定されたデバイスタイプがワードデバイスかどうかを判定
    /// </summary>
    public bool IsWordDevice(string deviceType)
    {
        var upperDeviceType = deviceType.ToUpper();
        return _seriesInfo.SupportedDevices.ContainsKey(upperDeviceType)
            ? _seriesInfo.SupportedDevices[upperDeviceType].IsWordDevice
            : false;
    }

    /// <summary>
    /// サポートされているデバイスのリストを取得
    /// </summary>
    public IReadOnlyDictionary<string, (byte Code, bool IsWordDevice)> GetSupportedDevices()
    {
        return _seriesInfo.SupportedDevices;
    }
}