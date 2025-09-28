using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;

namespace PLCUnifiedSimulator.Simulators;

/// <summary>
/// 三菱MCプロトコルシミュレータ
/// </summary>
public class MitsubishiMCSimulator : PLCSimulatorBase
{
    private readonly MitsubishiMCProtocol _protocol;
    private readonly MitsubishiPLCSeriesInfo _seriesInfo;

    public MitsubishiPLCSeries PLCSeries { get; }
    public override IPLCProtocol Protocol => _protocol;

    public MitsubishiMCSimulator(MitsubishiPLCSeries series = MitsubishiPLCSeries.QJ71E71_Binary_Station1, ILogger? logger = null)
        : base(logger)
    {
        PLCSeries = series;
        _protocol = new MitsubishiMCProtocol(series, logger);
        _seriesInfo = MitsubishiPLCSeriesInfo.GetSeriesInfo(series);
    }

    protected override async Task HandleUdpPacketAsync(byte[] data, System.Net.IPEndPoint remoteEndPoint, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("UDPパケット処理を開始します: {RemoteEndPoint}, データサイズ: {Size} bytes", remoteEndPoint, data.Length);
            var response = ProcessMCRequest(data, data.Length);
            if (response.Length > 0 && _udpListener != null)
            {
                await _udpListener.SendAsync(response, remoteEndPoint);
                _logger.LogDebug("UDPレスポンスを送信しました: {RemoteEndPoint}, レスポンスサイズ: {Size} bytes", remoteEndPoint, response.Length);
            }
            else
            {
                _logger.LogWarning("UDPレスポンスが空のため送信をスキップしました: {RemoteEndPoint}", remoteEndPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MC UDPパケット処理中にエラーが発生しました: {RemoteEndPoint}", remoteEndPoint);
        }
    }

    protected override async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];
        var remoteEndPoint = client.Client.RemoteEndPoint;

        try
        {
            _logger.LogDebug("TCPクライアント処理を開始します: {RemoteEndPoint}", remoteEndPoint);
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    _logger.LogDebug("TCPクライアントから切断を検知しました: {RemoteEndPoint}", remoteEndPoint);
                    break;
                }

                _logger.LogDebug("TCPデータを受信しました: {RemoteEndPoint}, サイズ: {Size} bytes", remoteEndPoint, bytesRead);
                var response = ProcessMCRequest(buffer, bytesRead);
                if (response.Length > 0)
                {
                    await stream.WriteAsync(response, cancellationToken);
                    _logger.LogDebug("TCPレスポンスを送信しました: {RemoteEndPoint}, レスポンスサイズ: {Size} bytes", remoteEndPoint, response.Length);
                }
                else
                {
                    _logger.LogWarning("TCPレスポンスが空のため送信をスキップしました: {RemoteEndPoint}", remoteEndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MC TCPクライアント処理中にエラーが発生しました: {RemoteEndPoint}", remoteEndPoint);
        }
    }

    private byte[] ProcessMCRequest(byte[] request, int length)
    {
        try
        {
            _logger.LogDebug("MC要求処理を開始します: データサイズ {Length} bytes", length);

            if (length < 21)
            {
                _logger.LogWarning("MC要求データ長が不足しています: 期待 21 bytes以上, 実際 {Length} bytes", length);
                return CreateMCErrorResponse(0xC059); // データ長異常
            }

            // MCプロトコルヘッダー解析
            var subHeader = Encoding.ASCII.GetString(request, 0, 4);
            if (subHeader != "5000")
            {
                _logger.LogWarning("MC要求フレーム異常: サブヘッダー {SubHeader} (期待: 5000)", subHeader);
                return CreateMCErrorResponse(0xC050); // フレーム異常
            }

            var command = BitConverter.ToUInt16(request, 15);
            var subCommand = BitConverter.ToUInt16(request, 17);

            _logger.LogDebug("MCコマンドを解析しました: コマンド 0x{Command:X4}, サブコマンド 0x{SubCommand:X4}", command, subCommand);

            var response = command switch
            {
                0x0401 => ProcessReadRequest(request, length),   // バッチ読み出し
                0x1401 => ProcessWriteRequest(request, length),  // バッチ書き込み
                _ => CreateMCErrorResponse(0xC05C) // コマンド異常
            };

            _logger.LogDebug("MC要求処理が完了しました: レスポンスサイズ {ResponseSize} bytes", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MC要求処理中にエラーが発生しました");
            return CreateMCErrorResponse(0xC070); // その他エラー
        }
    }

    private byte[] ProcessReadRequest(byte[] request, int length)
    {
        if (length < 26)
        {
            _logger.LogWarning("MC読み取り要求データ長が不足しています: 期待 26 bytes以上, 実際 {Length} bytes", length);
            return CreateMCErrorResponse(0xC059);
        }

        try
        {
            // デバイス情報を取得
            var deviceAddress = BitConverter.ToInt32(request, 19) & 0xFFFFFF; // 3バイト
            var deviceCode = request[22];
            var deviceCount = BitConverter.ToUInt16(request, 23);

            var deviceType = GetDeviceTypeFromCode(deviceCode);

            _logger.LogDebug("MC読み取り要求を解析しました: デバイスコード 0x{DeviceCode:X2}, アドレス {DeviceAddress}, カウント {DeviceCount}",
                deviceCode, deviceAddress, deviceCount);

            // サポートされていないデバイスの場合はエラーを返す
            if (deviceType == null)
            {
                _logger.LogWarning("未サポートデバイスコードです: 0x{DeviceCode:X2} ({SeriesDescription})", deviceCode, _seriesInfo.Description);
                return CreateMCErrorResponse(0xC058); // 指定デバイスなし
            }

            var responseData = new List<byte>();

            // 指定されたデバイスからデータを読み取り
            for (int i = 0; i < deviceCount; i++)
            {
                var address = new PLCAddress(deviceType, deviceAddress + i, 1);
                var data = GetDeviceValue(address) ?? new byte[] { 0x00, 0x00 }; // デフォルト値
                responseData.AddRange(data);
                _logger.LogDebug("デバイス {DeviceType}{DeviceAddress} のデータを読み取りました: {Data}",
                    deviceType, deviceAddress + i, BitConverter.ToString(data));
            }

            var response = CreateMCSuccessResponse(responseData.ToArray());
            _logger.LogDebug("MC読み取り要求を正常に処理しました: {DeviceCount} デバイス, レスポンスサイズ {ResponseSize} bytes",
                deviceCount, response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MC読み取り要求処理中にエラーが発生しました");
            return CreateMCErrorResponse(0xC070);
        }
    }

    private byte[] ProcessWriteRequest(byte[] request, int length)
    {
        if (length < 26)
        {
            _logger.LogWarning("MC書き込み要求データ長が不足しています: 期待 26 bytes以上, 実際 {Length} bytes", length);
            return CreateMCErrorResponse(0xC059);
        }

        try
        {
            // デバイス情報を取得
            var deviceAddress = BitConverter.ToInt32(request, 19) & 0xFFFFFF; // 3バイト
            var deviceCode = request[22];
            var deviceCount = BitConverter.ToUInt16(request, 23);

            var deviceType = GetDeviceTypeFromCode(deviceCode);

            _logger.LogDebug("MC書き込み要求を解析しました: デバイスコード 0x{DeviceCode:X2}, アドレス {DeviceAddress}, カウント {DeviceCount}",
                deviceCode, deviceAddress, deviceCount);

            // サポートされていないデバイスの場合はエラーを返す
            if (deviceType == null)
            {
                _logger.LogWarning("未サポートデバイスコードです: 0x{DeviceCode:X2} ({SeriesDescription})", deviceCode, _seriesInfo.Description);
                return CreateMCErrorResponse(0xC058); // 指定デバイスなし
            }

            var dataOffset = 25;

            // 指定されたデバイスにデータを書き込み
            for (int i = 0; i < deviceCount; i++)
            {
                if (dataOffset + 2 > length)
                {
                    _logger.LogWarning("MC書き込み要求データが不足しています: オフセット {DataOffset}, 要求長 {Length}", dataOffset, length);
                    break;
                }

                var address = new PLCAddress(deviceType, deviceAddress + i, 1);
                var data = new byte[] { request[dataOffset], request[dataOffset + 1] };
                SetDeviceValue(address, data);
                _logger.LogDebug("デバイス {DeviceType}{DeviceAddress} にデータを書き込みました: {Data}",
                    deviceType, deviceAddress + i, BitConverter.ToString(data));
                dataOffset += 2;
            }

            var response = CreateMCSuccessResponse(Array.Empty<byte>());
            _logger.LogDebug("MC書き込み要求を正常に処理しました: {DeviceCount} デバイス", deviceCount);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MC書き込み要求処理中にエラーが発生しました");
            return CreateMCErrorResponse(0xC070);
        }
    }

    private string? GetDeviceTypeFromCode(byte deviceCode)
    {
        // シリーズ情報から対応するデバイスタイプを検索
        foreach (var device in _seriesInfo.SupportedDevices)
        {
            if (device.Value.Code == deviceCode)
            {
                return device.Key;
            }
        }

        // サポートされていないデバイスの場合はnullを返す
        return null;
    }

    /// <summary>
    /// 指定されたデバイスコードがサポートされているかチェック
    /// </summary>
    /// <param name="deviceCode">デバイスコード</param>
    /// <returns>サポートされている場合はtrue</returns>
    public bool IsDeviceCodeSupported(byte deviceCode)
    {
        return GetDeviceTypeFromCode(deviceCode) != null;
    }

    /// <summary>
    /// サポートされているデバイスコードの一覧を取得
    /// </summary>
    /// <returns>デバイスコードの配列</returns>
    public byte[] GetSupportedDeviceCodes()
    {
        return _seriesInfo.SupportedDevices.Values.Select(d => d.Code).ToArray();
    }

    /// <summary>
    /// サポートされているデバイスの一覧を取得
    /// </summary>
    public IReadOnlyDictionary<string, (byte Code, bool IsWordDevice)> GetSupportedDevices()
    {
        return _seriesInfo.SupportedDevices;
    }

    /// <summary>
    /// PLCシリーズの説明を取得
    /// </summary>
    public string GetSeriesDescription()
    {
        return _seriesInfo.Description;
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