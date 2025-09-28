using System.Net.Sockets;
using System.Text;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// 三菱MCプロトコルシミュレータ
/// </summary>
public class MitsubishiMCSimulator : PLCSimulatorBase
{
    private readonly MitsubishiMCProtocol _protocol = new();

    public override IPLCProtocol Protocol => _protocol;

    protected override async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        try
        {
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0) break;

                var response = ProcessMCRequest(buffer, bytesRead);
                if (response.Length > 0)
                {
                    await stream.WriteAsync(response, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MCクライアント処理エラー: {ex.Message}");
        }
    }

    private byte[] ProcessMCRequest(byte[] request, int length)
    {
        try
        {
            if (length < 21) return CreateMCErrorResponse(0xC059); // データ長異常

            // MCプロトコルヘッダー解析
            var subHeader = Encoding.ASCII.GetString(request, 0, 4);
            if (subHeader != "5000") return CreateMCErrorResponse(0xC050); // フレーム異常

            var command = BitConverter.ToUInt16(request, 15);
            var subCommand = BitConverter.ToUInt16(request, 17);

            return command switch
            {
                0x0401 => ProcessReadRequest(request, length),   // バッチ読み出し
                0x1401 => ProcessWriteRequest(request, length),  // バッチ書き込み
                _ => CreateMCErrorResponse(0xC05C) // コマンド異常
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MC要求処理エラー: {ex.Message}");
            return CreateMCErrorResponse(0xC070); // その他エラー
        }
    }

    private byte[] ProcessReadRequest(byte[] request, int length)
    {
        if (length < 26) return CreateMCErrorResponse(0xC059);

        try
        {
            // デバイス情報を取得
            var deviceAddress = BitConverter.ToInt32(request, 19) & 0xFFFFFF; // 3バイト
            var deviceCode = request[22];
            var deviceCount = BitConverter.ToUInt16(request, 23);

            var deviceType = GetDeviceTypeFromCode(deviceCode);
            var responseData = new List<byte>();

            // 指定されたデバイスからデータを読み取り
            for (int i = 0; i < deviceCount; i++)
            {
                var address = new PLCAddress(deviceType, deviceAddress + i, 1);
                var data = GetDeviceValue(address) ?? new byte[] { 0x00, 0x00 }; // デフォルト値
                responseData.AddRange(data);
            }

            return CreateMCSuccessResponse(responseData.ToArray());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MC読み取り処理エラー: {ex.Message}");
            return CreateMCErrorResponse(0xC070);
        }
    }

    private byte[] ProcessWriteRequest(byte[] request, int length)
    {
        if (length < 26) return CreateMCErrorResponse(0xC059);

        try
        {
            // デバイス情報を取得
            var deviceAddress = BitConverter.ToInt32(request, 19) & 0xFFFFFF; // 3バイト
            var deviceCode = request[22];
            var deviceCount = BitConverter.ToUInt16(request, 23);

            var deviceType = GetDeviceTypeFromCode(deviceCode);
            var dataOffset = 25;

            // 指定されたデバイスにデータを書き込み
            for (int i = 0; i < deviceCount; i++)
            {
                if (dataOffset + 2 > length) break;

                var address = new PLCAddress(deviceType, deviceAddress + i, 1);
                var data = new byte[] { request[dataOffset], request[dataOffset + 1] };
                SetDeviceValue(address, data);
                dataOffset += 2;
            }

            return CreateMCSuccessResponse(Array.Empty<byte>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MC書き込み処理エラー: {ex.Message}");
            return CreateMCErrorResponse(0xC070);
        }
    }

    private string GetDeviceTypeFromCode(byte deviceCode)
    {
        return deviceCode switch
        {
            0x90 => "D",  // データレジスタ
            0x9C => "X",  // 入力リレー
            0x9D => "Y",  // 出力リレー
            0xA8 => "M",  // 内部リレー
            0xA0 => "B",  // リンクリレー
            0x93 => "F",  // ラッチリレー
            0x94 => "V",  // エッジリレー
            0x98 => "S",  // ステップリレー
            0xB4 => "W",  // リンクレジスタ
            0xAF => "R",  // ファイルレジスタ
            0xCC => "Z",  // インデックスレジスタ
            _ => "D"      // デフォルト
        };
    }

    private byte[] CreateMCSuccessResponse(byte[] data)
    {
        var response = new List<byte>();
        
        // 応答ヘッダ
        response.AddRange(Encoding.ASCII.GetBytes("D000")); // サブヘッダ
        response.Add(0x00); // 応答元ネットワーク番号
        response.Add(0xFF); // 応答元局番号
        response.AddRange(BitConverter.GetBytes((ushort)0x03FF)); // 応答元ユニットI/O番号
        response.Add(0x00); // 応答元マルチドロップ局番号
        response.AddRange(BitConverter.GetBytes((ushort)(2 + data.Length))); // 応答データ長
        response.AddRange(BitConverter.GetBytes((ushort)0x0000)); // エラーコード（正常）
        
        // データ
        response.AddRange(data);

        return response.ToArray();
    }

    private byte[] CreateMCErrorResponse(ushort errorCode)
    {
        var response = new List<byte>();
        
        // 応答ヘッダ
        response.AddRange(Encoding.ASCII.GetBytes("D000")); // サブヘッダ
        response.Add(0x00); // 応答元ネットワーク番号
        response.Add(0xFF); // 応答元局番号
        response.AddRange(BitConverter.GetBytes((ushort)0x03FF)); // 応答元ユニットI/O番号
        response.Add(0x00); // 応答元マルチドロップ局番号
        response.AddRange(BitConverter.GetBytes((ushort)2)); // 応答データ長
        response.AddRange(BitConverter.GetBytes(errorCode)); // エラーコード

        return response.ToArray();
    }
}