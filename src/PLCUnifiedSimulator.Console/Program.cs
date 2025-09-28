using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Protocols.Omron;
using PLCUnifiedSimulator.Simulators;

namespace PLCUnifiedSimulator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("=== PLC Unified Simulator ===");
        System.Console.WriteLine("1. 三菱MCプロトコルシミュレータを開始");
        System.Console.WriteLine("2. オムロンFINSプロトコルシミュレータを開始");
        System.Console.WriteLine("3. クライアントテスト");
        System.Console.WriteLine("q. 終了");
        System.Console.Write("選択してください: ");

        var choice = System.Console.ReadLine();

        switch (choice?.ToLower())
        {
            case "1":
                await RunMitsubishiSimulator();
                break;
            case "2":
                await RunOmronSimulator();
                break;
            case "3":
                await RunClientTest();
                break;
            case "q":
                break;
            default:
                System.Console.WriteLine("無効な選択です。");
                break;
        }
    }

    static async Task RunMitsubishiSimulator()
    {
        var simulator = new MitsubishiMCSimulator();
        
        System.Console.WriteLine("三菱MCプロトコルシミュレータを開始しています...");
        
        // 初期データを設定
        simulator.SetDeviceValue(new PLCAddress("D", 0, 1), BitConverter.GetBytes((short)1234));
        simulator.SetDeviceValue(new PLCAddress("D", 1, 1), BitConverter.GetBytes((short)5678));
        simulator.SetDeviceValue(new PLCAddress("M", 0, 1), new byte[] { 0x01, 0x00 });
        simulator.SetDeviceValue(new PLCAddress("X", 0, 1), new byte[] { 0x00, 0x00 });

        await simulator.StartAsync(5007);

        System.Console.WriteLine("シミュレータが実行中です。Enterキーで停止します。");
        System.Console.ReadLine();

        await simulator.StopAsync();
        simulator.Dispose();
    }

    static async Task RunOmronSimulator()
    {
        var simulator = new OmronFINSSimulator();
        
        System.Console.WriteLine("オムロンFINSプロトコルシミュレータを開始しています...");
        
        // 初期データを設定
        simulator.SetDeviceValue(new PLCAddress("D", 0, 1), BitConverter.GetBytes((short)9999));
        simulator.SetDeviceValue(new PLCAddress("D", 1, 1), BitConverter.GetBytes((short)1111));
        simulator.SetDeviceValue(new PLCAddress("C", 0, 1), new byte[] { 0x01, 0x00 });
        simulator.SetDeviceValue(new PLCAddress("W", 0, 1), BitConverter.GetBytes((short)2222));

        await simulator.StartAsync(9600);

        System.Console.WriteLine("シミュレータが実行中です。Enterキーで停止します。");
        System.Console.ReadLine();

        await simulator.StopAsync();
        simulator.Dispose();
    }

    static async Task RunClientTest()
    {
        System.Console.WriteLine("クライアントテストを開始します。");
        System.Console.WriteLine("1. 三菱MCプロトコルテスト");
        System.Console.WriteLine("2. オムロンFINSプロトコルテスト");
        System.Console.Write("選択してください: ");

        var choice = System.Console.ReadLine();

        switch (choice)
        {
            case "1":
                await TestMitsubishiClient();
                break;
            case "2":
                await TestOmronClient();
                break;
            default:
                System.Console.WriteLine("無効な選択です。");
                break;
        }
    }

    static async Task TestMitsubishiClient()
    {
        var client = new MitsubishiMCProtocol();
        
        try
        {
            System.Console.WriteLine("三菱PLCに接続中...");
            if (await client.ConnectAsync("127.0.0.1", 5007))
            {
                System.Console.WriteLine("接続成功！");

                // 読み取りテスト
                System.Console.WriteLine("\n--- 読み取りテスト ---");
                var readResult = await client.ReadAsync(new PLCAddress("D", 0, 2));
                if (readResult != null)
                {
                    var value1 = BitConverter.ToInt16(readResult.Data, 0);
                    var value2 = BitConverter.ToInt16(readResult.Data, 2);
                    System.Console.WriteLine($"D0: {value1}");
                    System.Console.WriteLine($"D1: {value2}");
                }

                // 書き込みテスト
                System.Console.WriteLine("\n--- 書き込みテスト ---");
                var writeData = BitConverter.GetBytes((short)7890);
                if (await client.WriteAsync(new PLCAddress("D", 10, 1), writeData))
                {
                    System.Console.WriteLine("D10に7890を書き込み成功");
                    
                    // 書き込み結果確認
                    var confirmResult = await client.ReadAsync(new PLCAddress("D", 10, 1));
                    if (confirmResult != null)
                    {
                        var confirmValue = BitConverter.ToInt16(confirmResult.Data, 0);
                        System.Console.WriteLine($"確認: D10 = {confirmValue}");
                    }
                }
            }
            else
            {
                System.Console.WriteLine("接続に失敗しました。");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"エラー: {ex.Message}");
        }
        finally
        {
            await client.DisconnectAsync();
            client.Dispose();
        }

        System.Console.WriteLine("\nテスト完了。Enterキーで戻ります。");
        System.Console.ReadLine();
    }

    static async Task TestOmronClient()
    {
        var client = new OmronFINSProtocol();
        
        try
        {
            System.Console.WriteLine("オムロンPLCに接続中...");
            if (await client.ConnectAsync("127.0.0.1", 9600))
            {
                System.Console.WriteLine("接続成功！");

                // 読み取りテスト
                System.Console.WriteLine("\n--- 読み取りテスト ---");
                var readResult = await client.ReadAsync(new PLCAddress("D", 0, 2));
                if (readResult != null)
                {
                    var value1 = BitConverter.ToInt16(readResult.Data, 0);
                    var value2 = BitConverter.ToInt16(readResult.Data, 2);
                    System.Console.WriteLine($"D0: {value1}");
                    System.Console.WriteLine($"D1: {value2}");
                }

                // 書き込みテスト
                System.Console.WriteLine("\n--- 書き込みテスト ---");
                var writeData = BitConverter.GetBytes((short)3333);
                if (await client.WriteAsync(new PLCAddress("D", 10, 1), writeData))
                {
                    System.Console.WriteLine("D10に3333を書き込み成功");
                    
                    // 書き込み結果確認
                    var confirmResult = await client.ReadAsync(new PLCAddress("D", 10, 1));
                    if (confirmResult != null)
                    {
                        var confirmValue = BitConverter.ToInt16(confirmResult.Data, 0);
                        System.Console.WriteLine($"確認: D10 = {confirmValue}");
                    }
                }
            }
            else
            {
                System.Console.WriteLine("接続に失敗しました。");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"エラー: {ex.Message}");
        }
        finally
        {
            await client.DisconnectAsync();
            client.Dispose();
        }

        System.Console.WriteLine("\nテスト完了。Enterキーで戻ります。");
        System.Console.ReadLine();
    }
}