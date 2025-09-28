using System.Net.Sockets;
using System.Text;
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

    public MitsubishiMCProtocol(MitsubishiPLCSeries series = MitsubishiPLCSeries.QJ71E71_Binary_Station1)
    {
        PLCSeries = series;
        _seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
    }

    public override async Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port, cancellationToken);
            _stream = _tcpClient.GetStream();
            _isConnected = true;
            return true;
        }
        catch
        {
            await DisconnectAsync();
            return false;
        }
    }

    public override async Task<bool> ConnectUdpAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        // UDPは接続レス型プロトコルのため、常に成功とする
        _isConnected = true;
        await Task.CompletedTask;
        return true;
    }

    public override async Task DisconnectAsync()
    {
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
        await Task.CompletedTask;
    }

    public override async Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _stream == null)
            return null;

        try
        {
            // デバイスアクセス前の検証
            ValidateDeviceAccess(address);

            var request = CreateReadRequest(address);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[1024];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);
            
            if (IsValidResponse(response, bytesRead))
            {
                var data = ExtractDataFromResponse(response, bytesRead, address.Size * 2); // 2 bytes per word
                return new PLCData(address, data);
            }
            
            return null;
        }
        catch (NotSupportedException)
        {
            // 未サポートデバイスの例外は再スロー
            throw;
        }
        catch (ArgumentException)
        {
            // 引数エラーの例外は再スロー
            throw;
        }
        catch
        {
            return null;
        }
    }

    public override async Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _stream == null)
            return false;

        try
        {
            // デバイスアクセス前の検証
            ValidateDeviceAccess(address);

            var request = CreateWriteRequest(address, data);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[256];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);
            
            return IsValidResponse(response, bytesRead);
        }
        catch (NotSupportedException)
        {
            // 未サポートデバイスの例外は再スロー
            throw;
        }
        catch (ArgumentException)
        {
            // 引数エラーの例外は再スロー
            throw;
        }
        catch
        {
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