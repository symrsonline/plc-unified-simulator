using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Omron;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// オムロンFINSプロトコルシミュレータ
/// </summary>
public class OmronFINSSimulator : PLCSimulatorBase
{
    private readonly OmronFINSProtocol _protocol = new();
    private readonly Dictionary<NetworkStream, byte> _clientNodes = new();
    private byte _nextNodeAddress = 0x01;

    public override IPLCProtocol Protocol => _protocol;

    public OmronFINSSimulator(ILogger? logger = null) : base(logger)
    {
    }

    protected override async Task HandleUdpPacketAsync(byte[] data, System.Net.IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        try
        {
            // UDP接続では接続確立フェーズをスキップし、直接FINSコマンドを処理
            byte[] response;

            // UDP FINSの場合は通常のFINS応答フレームを返す
            if (data.Length >= 12) // 最小FINS UDPフレーム長
            {
                response = ProcessFINSUdpRequest(data, data.Length);
            }
            else
            {
                response = CreateFINSErrorResponse(0x01, 0x01);
            }

            if (response.Length > 0 && _udpListener != null)
            {
                await _udpListener.SendAsync(response, remoteEndPoint);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS UDP パケット処理エラー: {ex.Message}");
        }
    }

    protected override async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        try
        {
            // FINS接続確立
            if (!await HandleFINSConnection(stream, cancellationToken))
            {
                Console.WriteLine("FINS接続確立に失敗しました。");
                return;
            }

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                var response = ProcessFINSRequest(stream, buffer, bytesRead);
                if (response.Length > 0)
                {
                    await stream.WriteAsync(response, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINSクライアント処理エラー: {ex.Message}");
        }
        finally
        {
            lock (_clientNodes)
            {
                _clientNodes.Remove(stream);
            }
        }
    }

    private async Task<bool> HandleFINSConnection(NetworkStream stream, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[20];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

            if (bytesRead >= 20 &&
                buffer[0] == 0x46 && buffer[1] == 0x49 &&
                buffer[2] == 0x4E && buffer[3] == 0x53) // "FINS"
            {
                var clientNodeAddress = buffer[19];

                lock (_clientNodes)
                {
                    _clientNodes[stream] = clientNodeAddress;
                }

                // 接続応答
                var response = new byte[]
                {
                    0x46, 0x49, 0x4E, 0x53, // "FINS"
                    0x00, 0x00, 0x00, 0x10, // Length
                    0x00, 0x00, 0x00, 0x01, // Command
                    0x00, 0x00, 0x00, 0x00, // Error code (success)
                    _nextNodeAddress, 0x00, 0x00, 0x00 // Server node address
                };

                await stream.WriteAsync(response, cancellationToken);
                Console.WriteLine($"FINSクライアント（ノード: {clientNodeAddress:X2}）が接続されました。");
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS接続処理エラー: {ex.Message}");
            return false;
        }
    }

    private byte[] ProcessFINSRequest(NetworkStream stream, byte[] request, int length)
    {
        try
        {
            if (length < 34) return CreateFINSErrorResponse(0x01, 0x01); // フレーム長異常

            // FINSヘッダー確認
            if (request[0] != 0x46 || request[1] != 0x49 ||
                request[2] != 0x4E || request[3] != 0x53) // "FINS"
            {
                return CreateFINSErrorResponse(0x01, 0x02); // フレーム異常
            }

            // FINSコマンド解析
            var commandCode1 = request[26];
            var commandCode2 = request[27];

            return (commandCode1, commandCode2) switch
            {
                (0x01, 0x01) => ProcessFINSReadRequest(request, length),
                (0x01, 0x02) => ProcessFINSWriteRequest(request, length),
                _ => CreateFINSErrorResponse(0x01, 0x01) // 未サポートコマンド
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS要求処理エラー: {ex.Message}");
            return CreateFINSErrorResponse(0x01, 0x03);
        }
    }

    private byte[] ProcessFINSReadRequest(byte[] request, int length)
    {
        if (length < 34) return CreateFINSErrorResponse(0x01, 0x01);

        try
        {
            var memoryAreaCode = request[28];
            var address = (ushort)((request[29] << 8) | request[30]);
            var bitPosition = request[31];
            var itemCount = (ushort)((request[32] << 8) | request[33]);

            var deviceType = GetDeviceTypeFromMemoryArea(memoryAreaCode);
            var responseData = new List<byte>();

            // 指定されたメモリ領域からデータを読み取り
            for (int i = 0; i < itemCount; i++)
            {
                var plcAddress = new PLCAddress(deviceType, address + i, 1);
                var data = GetDeviceValue(plcAddress) ?? new byte[] { 0x00, 0x00 };
                responseData.AddRange(data);
            }

            return CreateFINSSuccessResponse(request, responseData.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS読み取り処理エラー: {ex.Message}");
            return CreateFINSErrorResponse(0x01, 0x03);
        }
    }

    private byte[] ProcessFINSWriteRequest(byte[] request, int length)
    {
        if (length < 34) return CreateFINSErrorResponse(0x01, 0x01);

        try
        {
            var memoryAreaCode = request[28];
            var address = (ushort)((request[29] << 8) | request[30]);
            var bitPosition = request[31];
            var itemCount = (ushort)((request[32] << 8) | request[33]);

            var deviceType = GetDeviceTypeFromMemoryArea(memoryAreaCode);
            var dataOffset = 34;

            // 指定されたメモリ領域にデータを書き込み
            for (int i = 0; i < itemCount; i++)
            {
                if (dataOffset + 2 > length) break;

                var plcAddress = new PLCAddress(deviceType, address + i, 1);
                var data = new byte[] { request[dataOffset], request[dataOffset + 1] };
                SetDeviceValue(plcAddress, data);
                dataOffset += 2;
            }

            return CreateFINSSuccessResponse(request, Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS書き込み処理エラー: {ex.Message}");
            return CreateFINSErrorResponse(0x01, 0x03);
        }
    }

    private string GetDeviceTypeFromMemoryArea(byte memoryAreaCode)
    {
        return memoryAreaCode switch
        {
            0xb0 => "IO",   // 入出力リレー(チャネルI/O)
            0xb1 => "WR",   // 内部補助リレー
            0xb2 => "HR",   // 保持リレー
            0xb3 => "AR",   // 補助記憶リレー
            0x09 => "TS",   // タイマアップフラグ/カウンタアップフラグ
            0x89 => "TN",   // タイマ現在値/カウンタ現在値
            0x82 => "DM",   // データメモリ
            0x98 => "EM",   // 拡張メモリ(カレント)
            0xa0 => "EB",   // 拡張メモリ(バンク指定)
            0x06 => "TKB",  // タスクフラグ(ビット)
            0x46 => "TKS",  // タスクフラグ(ステータス)
            0xdc => "IR",   // インデックスレジスタ
            0xbc => "DR",   // データレジスタ
            // 後方互換性のため
            0x31 => "W",    // WR領域
            0x32 => "H",    // HR領域
            0x33 => "A",    // AR領域
            _ => "DM"        // デフォルト
        };
    }

    private byte[] CreateFINSSuccessResponse(byte[] originalRequest, byte[] data)
    {
        var response = new List<byte>();

        // FINSヘッダ
        response.AddRange(new byte[] { 0x46, 0x49, 0x4E, 0x53 }); // "FINS"
        response.AddRange(BitConverter.GetBytes(0x1A + data.Length).Reverse()); // Length (big endian)
        response.AddRange(BitConverter.GetBytes((uint)0x02).Reverse()); // Command (big endian)
        response.AddRange(BitConverter.GetBytes((uint)0x00).Reverse()); // Error code (big endian)

        // FINS応答フレーム
        response.Add(0xC0); // ICF (応答)
        response.Add(0x00); // RSV
        response.Add(0x02); // GCT
        response.Add(originalRequest[22]); // DNA (元の送信元)
        response.Add(originalRequest[23]); // DA1
        response.Add(originalRequest[24]); // DA2
        response.Add(originalRequest[19]); // SNA (元の宛先)
        response.Add(originalRequest[20]); // SA1
        response.Add(originalRequest[21]); // SA2
        response.Add(originalRequest[25]); // SID
        response.Add(0x00); // MRC (正常終了)
        response.Add(0x00); // SRC (正常終了)

        // データ
        response.AddRange(data);

        return response.ToArray();
    }

    private byte[] CreateFINSErrorResponse(byte mainResponseCode, byte subResponseCode)
    {
        return new byte[]
        {
            0x46, 0x49, 0x4E, 0x53, // "FINS"
            0x00, 0x00, 0x00, 0x1A, // Length
            0x00, 0x00, 0x00, 0x02, // Command
            0x00, 0x00, 0x00, 0x00, // Error code
            0xC0, 0x00, 0x02,       // ICF, RSV, GCT
            0x00, 0x00, 0x00,       // DNA, DA1, DA2
            0x01, 0x00, 0x00,       // SNA, SA1, SA2
            0x00,                   // SID
            mainResponseCode,       // MRC
            subResponseCode         // SRC
        };
    }

    private byte[] ProcessFINSUdpRequest(byte[] request, int length)
    {
        try
        {
            if (length < 12) return CreateFINSUdpErrorResponse(0x01, 0x01);

            // UDP FINS フレーム解析（TCPヘッダなし）
            var commandCode1 = request[10];
            var commandCode2 = request[11];

            return (commandCode1, commandCode2) switch
            {
                (0x01, 0x01) => ProcessFINSUdpReadRequest(request, length),
                (0x01, 0x02) => ProcessFINSUdpWriteRequest(request, length),
                _ => CreateFINSUdpErrorResponse(0x01, 0x01)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS UDP要求処理エラー: {ex.Message}");
            return CreateFINSUdpErrorResponse(0x01, 0x03);
        }
    }

    private byte[] ProcessFINSUdpReadRequest(byte[] request, int length)
    {
        if (length < 18) return CreateFINSUdpErrorResponse(0x01, 0x01);

        try
        {
            var memoryAreaCode = request[12];
            var address = (ushort)((request[13] << 8) | request[14]);
            var bitPosition = request[15];
            var itemCount = (ushort)((request[16] << 8) | request[17]);

            var deviceType = GetDeviceTypeFromMemoryArea(memoryAreaCode);
            var responseData = new List<byte>();

            for (int i = 0; i < itemCount; i++)
            {
                var plcAddress = new PLCAddress(deviceType, address + i, 1);
                var data = GetDeviceValue(plcAddress) ?? new byte[] { 0x00, 0x00 };
                responseData.AddRange(data);
            }

            return CreateFINSUdpSuccessResponse(request, responseData.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS UDP読み取り処理エラー: {ex.Message}");
            return CreateFINSUdpErrorResponse(0x01, 0x03);
        }
    }

    private byte[] ProcessFINSUdpWriteRequest(byte[] request, int length)
    {
        if (length < 18) return CreateFINSUdpErrorResponse(0x01, 0x01);

        try
        {
            var memoryAreaCode = request[12];
            var address = (ushort)((request[13] << 8) | request[14]);
            var bitPosition = request[15];
            var itemCount = (ushort)((request[16] << 8) | request[17]);

            var deviceType = GetDeviceTypeFromMemoryArea(memoryAreaCode);
            var dataOffset = 18;

            for (int i = 0; i < itemCount; i++)
            {
                if (dataOffset + 2 > length) break;

                var plcAddress = new PLCAddress(deviceType, address + i, 1);
                var data = new byte[] { request[dataOffset], request[dataOffset + 1] };
                SetDeviceValue(plcAddress, data);
                dataOffset += 2;
            }

            return CreateFINSUdpSuccessResponse(request, Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FINS UDP書き込み処理エラー: {ex.Message}");
            return CreateFINSUdpErrorResponse(0x01, 0x03);
        }
    }

    private byte[] CreateFINSUdpSuccessResponse(byte[] originalRequest, byte[] data)
    {
        var response = new List<byte>();

        // FINS UDPフレーム（TCPヘッダなし）
        response.Add(0xC0); // ICF (応答)
        response.Add(0x00); // RSV
        response.Add(0x02); // GCT
        response.Add(originalRequest[6]); // DNA (元の送信元)
        response.Add(originalRequest[7]); // DA1
        response.Add(originalRequest[8]); // DA2
        response.Add(originalRequest[3]); // SNA (元の宛先)
        response.Add(originalRequest[4]); // SA1
        response.Add(originalRequest[5]); // SA2
        response.Add(originalRequest[9]); // SID
        response.Add(0x00); // MRC (正常終了)
        response.Add(0x00); // SRC (正常終了)

        // データ
        response.AddRange(data);

        return response.ToArray();
    }

    private byte[] CreateFINSUdpErrorResponse(byte mainResponseCode, byte subResponseCode)
    {
        return new byte[]
        {
            0xC0, 0x00, 0x02,       // ICF, RSV, GCT
            0x00, 0x00, 0x00,       // DNA, DA1, DA2
            0x01, 0x00, 0x00,       // SNA, SA1, SA2
            0x00,                   // SID
            mainResponseCode,       // MRC
            subResponseCode         // SRC
        };
    }

    /// <summary>
    /// サポートされているデバイスの一覧を取得
    /// </summary>
    public IReadOnlyDictionary<string, byte> GetSupportedDevices()
    {
        return new Dictionary<string, byte>
        {
            ["IO"] = 0xb0,   // 入出力リレー(チャネルI/O)
            ["WR"] = 0xb1,   // 内部補助リレー
            ["HR"] = 0xb2,   // 保持リレー
            ["AR"] = 0xb3,   // 補助記憶リレー
            ["TS"] = 0x09,   // タイマアップフラグ/カウンタアップフラグ
            ["CS"] = 0x09,   // タイマアップフラグ/カウンタアップフラグ (TSと同じ)
            ["TN"] = 0x89,   // タイマ現在値/カウンタ現在値
            ["CN"] = 0x89,   // タイマ現在値/カウンタ現在値 (TNと同じ)
            ["DM"] = 0x82,   // データメモリ
            ["EM"] = 0x98,   // 拡張メモリ(カレント)
            ["EB"] = 0xa0,   // 拡張メモリ(バンク指定)
            ["TKB"] = 0x06,  // タスクフラグ(ビット)
            ["TKS"] = 0x46,  // タスクフラグ(ステータス)
            ["IR"] = 0xdc,   // インデックスレジスタ
            ["DR"] = 0xbc,   // データレジスタ
            ["W"] = 0x31,    // WR領域 (標準デバイス)
            ["H"] = 0x32,    // HR領域 (標準デバイス)
            ["A"] = 0x33,    // AR領域 (標準デバイス)
            ["C"] = 0x09     // カウンタ (TS/CSと同じ、標準デバイス)
        };
    }
}