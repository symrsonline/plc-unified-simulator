using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using PLCUnifiedSimulator.Core;

namespace PLCUnifiedSimulator.Protocols.Omron;

/// <summary>
/// オムロンFINSプロトコルの実装
/// </summary>
public class OmronFINSProtocol : PLCProtocolBase
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private UdpClient? _udpClient;
    private readonly object _lockObject = new();
    private byte _sourceNodeAddress = 0x01;
    private byte _destinationNodeAddress = 0x00;

    public override string ProtocolName => "OMRON FINS";
    public override int DefaultPort => 9600;

    public OmronFINSProtocol(ILogger? logger = null) : base(logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OmronFINSProtocol>.Instance)
    {
    }

    public override async Task<bool> ConnectAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port, cancellationToken);
            _stream = _tcpClient.GetStream();

            // FINS接続確立
            if (await EstablishFINSConnection())
            {
                _isConnected = true;
                return true;
            }

            await DisconnectAsync();
            return false;
        }
        catch
        {
            await DisconnectAsync();
            return false;
        }
    }

    public override async Task<bool> ConnectUdpAsync(string ipAddress, int port, CancellationToken cancellationToken = default)
    {
        try
        {
            _udpClient = new UdpClient();
            _udpClient.Connect(ipAddress, port);
            _isConnected = true;
            await Task.CompletedTask;
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
            _udpClient?.Close();
            _udpClient?.Dispose();
            _stream = null;
            _tcpClient = null;
            _udpClient = null;
            _isConnected = false;
        }
        await Task.CompletedTask;
    }

    public override async Task<PLCData?> ReadAsync(PLCAddress address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return null;

        try
        {
            var request = _udpClient == null ? CreateReadRequest(address) : CreateUdpReadRequest(address);

            byte[] response;
            int bytesRead;

            if (_stream != null)
            {
                await _stream.WriteAsync(request, cancellationToken);
                response = new byte[1024];
                bytesRead = await _stream.ReadAsync(response, cancellationToken);
            }
            else if (_udpClient != null)
            {
                await _udpClient.SendAsync(request);
                var udpResult = await _udpClient.ReceiveAsync();
                response = udpResult.Buffer;
                bytesRead = response.Length;
            }
            else
            {
                return null;
            }

            if ((_udpClient == null && IsValidFINSResponse(response, bytesRead)) ||
                (_udpClient != null && IsValidFINSUdpResponse(response, bytesRead)))
            {
                var data = (_udpClient == null)
                    ? ExtractDataFromResponse(response, bytesRead, address.Size * 2)
                    : ExtractDataFromUdpResponse(response, bytesRead, address.Size * 2);
                return new PLCData(address, data);
            }

            // デバッグ情報: 無効なレスポンスを受信した場合の生データを出力
            try
            {
                Console.WriteLine($"[OmronFINSProtocol] Invalid response ({bytesRead} bytes): {BitConverter.ToString(response, 0, Math.Min(bytesRead, response.Length))}");
            }
            catch { }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public override async Task<bool> WriteAsync(PLCAddress address, byte[] data, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return false;

        try
        {
            var request = _udpClient == null ? CreateWriteRequest(address, data) : CreateUdpWriteRequest(address, data);

            byte[] response;
            int bytesRead;

            if (_stream != null)
            {
                await _stream.WriteAsync(request, cancellationToken);
                response = new byte[256];
                bytesRead = await _stream.ReadAsync(response, cancellationToken);
                return IsValidFINSResponse(response, bytesRead);
            }
            else if (_udpClient != null)
            {
                await _udpClient.SendAsync(request);
                var udpResult = await _udpClient.ReceiveAsync();
                response = udpResult.Buffer;
                bytesRead = response.Length;
                var success = IsValidFINSUdpResponse(response, bytesRead);
                if (!success)
                {
                    // デバッグ情報: 無効なレスポンスを受信した場合の生データを出力
                    try
                    {
                        Console.WriteLine($"[OmronFINSProtocol] Invalid response ({bytesRead} bytes): {BitConverter.ToString(response, 0, Math.Min(bytesRead, response.Length))}");
                    }
                    catch { }
                }
                return success;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EstablishFINSConnection()
    {
        if (_stream == null) return false;

        try
        {
            // FINS接続要求フレーム
            var connectionFrame = new byte[]
            {
                0x46, 0x49, 0x4E, 0x53, // "FINS"
                0x00, 0x00, 0x00, 0x0C, // Length
                0x00, 0x00, 0x00, 0x00, // Command
                0x00, 0x00, 0x00, 0x00, // Error code
                0x00, 0x00, 0x00, 0x01  // Client node address
            };

            await _stream.WriteAsync(connectionFrame);

            var response = new byte[24];
            var bytesRead = await _stream.ReadAsync(response);

            if (bytesRead >= 24 && response[15] == 0x00) // エラーコードが0
            {
                _destinationNodeAddress = response[19]; // サーバーノードアドレス
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private byte[] CreateReadRequest(PLCAddress address)
    {
        var memoryArea = GetMemoryAreaCode(address.DeviceType);
        var addressBytes = BitConverter.GetBytes((ushort)address.Address);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(addressBytes);
        }

        var finsHeader = new byte[]
        {
            0x46, 0x49, 0x4E, 0x53, // "FINS"
            0x00, 0x00, 0x00, 0x1A, // Length
            0x00, 0x00, 0x00, 0x02, // Command
            0x00, 0x00, 0x00, 0x00  // Error code
        };

        var finsCommand = new byte[]
        {
            0x80, // ICF
            0x00, // RSV
            0x02, // GCT
            _destinationNodeAddress, // DNA
            0x00, // DA1
            0x00, // DA2
            _sourceNodeAddress, // SNA
            0x00, // SA1
            0x00, // SA2
            0x00, // SID
            0x01, 0x01, // Memory area read command
            memoryArea.Code, // Memory area code
            addressBytes[1], addressBytes[0], // Starting address
            0x00, // Bit position
            0x00, (byte)address.Size // Number of items to read
        };

        var frame = new byte[finsHeader.Length + finsCommand.Length];
        Array.Copy(finsHeader, 0, frame, 0, finsHeader.Length);
        Array.Copy(finsCommand, 0, frame, finsHeader.Length, finsCommand.Length);

        return frame;
    }

    private byte[] CreateWriteRequest(PLCAddress address, byte[] data)
    {
        var memoryArea = GetMemoryAreaCode(address.DeviceType);
        var addressBytes = BitConverter.GetBytes((ushort)address.Address);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(addressBytes);
        }

        var finsHeader = new byte[]
        {
            0x46, 0x49, 0x4E, 0x53, // "FINS"
            0x00, 0x00, 0x00, (byte)(0x1A + data.Length), // Length
            0x00, 0x00, 0x00, 0x02, // Command
            0x00, 0x00, 0x00, 0x00  // Error code
        };

        var finsCommand = new byte[]
        {
            0x80, // ICF
            0x00, // RSV
            0x02, // GCT
            _destinationNodeAddress, // DNA
            0x00, // DA1
            0x00, // DA2
            _sourceNodeAddress, // SNA
            0x00, // SA1
            0x00, // SA2
            0x00, // SID
            0x01, 0x02, // Memory area write command
            memoryArea.Code, // Memory area code
            addressBytes[1], addressBytes[0], // Starting address
            0x00, // Bit position
            0x00, (byte)address.Size // Number of items to write
        };

        var frame = new byte[finsHeader.Length + finsCommand.Length + data.Length];
        Array.Copy(finsHeader, 0, frame, 0, finsHeader.Length);
        Array.Copy(finsCommand, 0, frame, finsHeader.Length, finsCommand.Length);
        Array.Copy(data, 0, frame, finsHeader.Length + finsCommand.Length, data.Length);

        return frame;
    }

    private bool IsValidFINSResponse(byte[] response, int length)
    {
        if (length < 30) return false;

        // FINS応答の結果コードをチェック
        return response[28] == 0x00 && response[29] == 0x00;
    }

    // UDP (TCPヘッダなし) 用の応答検証
    private bool IsValidFINSUdpResponse(byte[] response, int length)
    {
        if (length < 12) return false;
        // UDP FINS応答: ICF(0xC0), RSV, GCT, DNA,DA1,DA2, SNA,SA1,SA2, SID, MRC,SRC
        // 正常は MRC/SRC が 0x00/0x00
        return response[10] == 0x00 && response[11] == 0x00;
    }

    private byte[] ExtractDataFromResponse(byte[] response, int length, int dataSize)
    {
        // データ部分は30バイト目から開始
        var data = new byte[dataSize];
        Array.Copy(response, 30, data, 0, Math.Min(dataSize, length - 30));
        return data;
    }

    // UDP FINS: データは12バイト目以降
    private byte[] ExtractDataFromUdpResponse(byte[] response, int length, int dataSize)
    {
        var data = new byte[dataSize];
        Array.Copy(response, 12, data, 0, Math.Min(dataSize, length - 12));
        return data;
    }

    private (byte Code, string Name) GetMemoryAreaCode(string deviceType)
    {
        return deviceType.ToUpper() switch
        {
            "IO" => (0xb0, "入出力リレー(チャネルI/O)"),
            "WR" => (0xb1, "内部補助リレー"),
            "HR" => (0xb2, "保持リレー"),
            "AR" => (0xb3, "補助記憶リレー"),
            "TS" => (0x09, "タイマアップフラグ"),
            "CS" => (0x09, "カウンタアップフラグ"),
            "TN" => (0x89, "タイマ現在値"),
            "CN" => (0x89, "カウンタ現在値"),
            "DM" => (0x82, "データメモリ"),
            "EM" => (0x98, "拡張メモリ(カレント)"),
            "EB" => (0xa0, "拡張メモリ(バンク指定)"),
            "TKB" => (0x06, "タスクフラグ(ビット)"),
            "TKS" => (0x46, "タスクフラグ(ステータス)"),
            "IR" => (0xdc, "インデックスレジスタ"),
            "DR" => (0xbc, "データレジスタ"),
            // 後方互換性のため
            "D" => (0x82, "DM領域"),
            "C" => (0x89, "CIO領域"),
            "W" => (0x31, "WR領域"),
            "H" => (0x32, "HR領域"),
            "A" => (0x33, "AR領域"),
            "T" => (0x09, "タイマ"),
            "CT" => (0x09, "カウンタ"),
            _ => (0x82, "不明")
        };
    }

    // --- UDP 専用フレーム組み立て ---
    private byte[] CreateUdpReadRequest(PLCAddress address)
    {
        var memoryArea = GetMemoryAreaCode(address.DeviceType);
        var addressBytes = BitConverter.GetBytes((ushort)address.Address);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(addressBytes);
        }

        // UDP FINSフレーム（TCPヘッダなし）
        var frame = new List<byte>
        {
            0x80, // ICF
            0x00, // RSV
            0x02, // GCT
            0x00, // DNA (未使用: ブロードキャスト/直接指定しない)
            0x00, // DA1
            0x00, // DA2
            _sourceNodeAddress, // SNA
            0x00, // SA1
            0x00, // SA2
            0x00, // SID
            0x01, 0x01, // メモリアクセス 読み出し
            memoryArea.Code,
            addressBytes[1], addressBytes[0],
            0x00, // ビット位置
            0x00, (byte)address.Size
        };

        return frame.ToArray();
    }

    private byte[] CreateUdpWriteRequest(PLCAddress address, byte[] data)
    {
        var memoryArea = GetMemoryAreaCode(address.DeviceType);
        var addressBytes = BitConverter.GetBytes((ushort)address.Address);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(addressBytes);
        }

        var frame = new List<byte>
        {
            0x80, // ICF
            0x00, // RSV
            0x02, // GCT
            0x00, // DNA
            0x00, // DA1
            0x00, // DA2
            _sourceNodeAddress, // SNA
            0x00, // SA1
            0x00, // SA2
            0x00, // SID
            0x01, 0x02, // メモリアクセス 書き込み
            memoryArea.Code,
            addressBytes[1], addressBytes[0],
            0x00, // ビット位置
            0x00, (byte)address.Size
        };

        frame.AddRange(data);
        return frame.ToArray();
    }
}