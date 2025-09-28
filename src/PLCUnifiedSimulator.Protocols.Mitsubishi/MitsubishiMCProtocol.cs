using System.Net.Sockets;
using System.Text;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Protocols.Mitsubishi;

/// <summary>
/// 三菱MCプロトコル（QシリーズおよびiQシリーズ）の実装
/// </summary>
public class MitsubishiMCProtocol : PLCProtocolBase
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly object _lockObject = new();

    public override string ProtocolName => "Mitsubishi MC Protocol";
    public override int DefaultPort => 5007;

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
    }

    public override async Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected || _stream == null)
            return null;

        try
        {
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
            var request = CreateWriteRequest(address, data);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[256];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);
            
            return IsValidResponse(response, bytesRead);
        }
        catch
        {
            return false;
        }
    }

    private byte[] CreateReadRequest(PLCAddress address)
    {
        // MCプロトコル バッチ読み出し要求フレーム
        var frame = new List<byte>();
        
        // フレームヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.Add(0x00); // 要求先ネットワーク番号
        frame.Add(0xFF); // 要求先局番号
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF)); // 要求先ユニットI/O番号
        frame.Add(0x00); // 要求先マルチドロップ局番号
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

    private byte[] CreateWriteRequest(PLCAddress address, byte[] data)
    {
        // MCプロトコル バッチ書き込み要求フレーム
        var frame = new List<byte>();
        
        // フレームヘッダ
        frame.AddRange(Encoding.ASCII.GetBytes("5000")); // サブヘッダ
        frame.Add(0x00); // 要求先ネットワーク番号
        frame.Add(0xFF); // 要求先局番号
        frame.AddRange(BitConverter.GetBytes((ushort)0x03FF)); // 要求先ユニットI/O番号
        frame.Add(0x00); // 要求先マルチドロップ局番号
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
        return deviceType.ToUpper() switch
        {
            "D" => (0x90, "データレジスタ"),
            "X" => (0x9C, "入力リレー"),
            "Y" => (0x9D, "出力リレー"),
            "M" => (0x90, "内部リレー"),
            "B" => (0xA0, "リンクリレー"),
            "F" => (0x93, "ラッチリレー"),
            "V" => (0x94, "エッジリレー"),
            "S" => (0x98, "ステップリレー"),
            "W" => (0xB4, "リンクレジスタ"),
            "R" => (0xAF, "ファイルレジスタ"),
            "Z" => (0xCC, "インデックスレジスタ"),
            _ => (0x90, "不明")
        };
    }
}