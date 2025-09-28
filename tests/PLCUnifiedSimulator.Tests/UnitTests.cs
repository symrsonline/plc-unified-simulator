using FluentAssertions;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Protocols.Omron;
using PLCUnifiedSimulator.Simulators;
using System.Net.Sockets;
using Xunit;

namespace PLCUnifiedSimulator.Tests;

public class PLCDataTests
{
    [Fact]
    public void PLCData_Should_Store_Address_And_Data()
    {
        // Arrange
        var address = new PLCAddress("D", 100, 1);
        var data = BitConverter.GetBytes((short)1234);

        // Act
        var plcData = new PLCData(address, data);

        // Assert
        plcData.Address.Should().Be(address);
        plcData.Data.Should().BeEquivalentTo(data);
        plcData.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PLCData_GetValue_Should_Return_Correct_Short()
    {
        // Arrange
        var address = new PLCAddress("D", 100, 1);
        var expectedValue = (short)1234;
        var data = BitConverter.GetBytes(expectedValue);
        var plcData = new PLCData(address, data);

        // Act
        var actualValue = plcData.GetValue<short>();

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void PLCData_GetValue_Should_Return_Correct_Int()
    {
        // Arrange
        var address = new PLCAddress("D", 100, 2);
        var expectedValue = 1234567;
        var data = BitConverter.GetBytes(expectedValue);
        var plcData = new PLCData(address, data);

        // Act
        var actualValue = plcData.GetValue<int>();

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void PLCData_GetValue_Should_Return_Correct_Bool()
    {
        // Arrange
        var address = new PLCAddress("M", 0, 1);
        var data = new byte[] { 0x01 };
        var plcData = new PLCData(address, data);

        // Act
        var actualValue = plcData.GetValue<bool>();

        // Assert
        actualValue.Should().BeTrue();
    }
}

public class PLCAddressTests
{
    [Fact]
    public void PLCAddress_Should_Initialize_Properties()
    {
        // Arrange & Act
        var address = new PLCAddress("D", 100, 5);

        // Assert
        address.DeviceType.Should().Be("D");
        address.Address.Should().Be(100);
        address.Size.Should().Be(5);
    }

    [Fact]
    public void PLCAddress_ToString_Should_Return_Formatted_String()
    {
        // Arrange
        var address = new PLCAddress("D", 100, 1);

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be("D100");
    }
}

public class MitsubishiMCProtocolTests
{
    [Fact]
    public void MitsubishiMCProtocol_Should_Have_Correct_Properties()
    {
        // Arrange & Act
        var protocol = new MitsubishiMCProtocol();

        // Assert
        protocol.ProtocolName.Should().StartWith("Mitsubishi MC Protocol");
        protocol.DefaultPort.Should().Be(5000);
        protocol.IsConnected.Should().BeFalse();
    }
}

public class OmronFINSProtocolTests
{
    [Fact]
    public void OmronFINSProtocol_Should_Have_Correct_Properties()
    {
        // Arrange & Act
        var protocol = new OmronFINSProtocol();

        // Assert
        protocol.ProtocolName.Should().Be("OMRON FINS");
        protocol.DefaultPort.Should().Be(9600);
        protocol.IsConnected.Should().BeFalse();
    }
}

public class MitsubishiMCSimulatorTests
{
    [Fact]
    public void MitsubishiMCSimulator_Should_Initialize_Correctly()
    {
        // Arrange & Act
        using var simulator = new MitsubishiMCSimulator();

        // Assert
        simulator.Protocol.Should().NotBeNull();
        simulator.Protocol.ProtocolName.Should().StartWith("Mitsubishi MC Protocol");
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void MitsubishiMCSimulator_Should_Store_And_Retrieve_Device_Values()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        var address = new PLCAddress("D", 100, 1);
        var expectedValue = BitConverter.GetBytes((short)1234);

        // Act
        simulator.SetDeviceValue(address, expectedValue);
        var actualValue = simulator.GetDeviceValue(address);

        // Assert
        actualValue.Should().BeEquivalentTo(expectedValue);
    }
}

public class OmronFINSSimulatorTests
{
    [Fact]
    public void OmronFINSSimulator_Should_Initialize_Correctly()
    {
        // Arrange & Act
        using var simulator = new OmronFINSSimulator();

        // Assert
        simulator.Protocol.Should().NotBeNull();
        simulator.Protocol.ProtocolName.Should().Be("OMRON FINS");
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void OmronFINSSimulator_Should_Store_And_Retrieve_Device_Values()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        var address = new PLCAddress("D", 100, 1);
        var expectedValue = BitConverter.GetBytes((short)5678);

        // Act
        simulator.SetDeviceValue(address, expectedValue);
        var actualValue = simulator.GetDeviceValue(address);

        // Assert
        actualValue.Should().BeEquivalentTo(expectedValue);
    }
}

public class UDPCommunicationTests
{
    [Fact]
    public async Task MitsubishiMCSimulator_Should_Support_UDP_Start()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act & Assert
        var startTask = simulator.StartUdpAsync(6000);
        startTask.Should().NotBeNull();
        
        // UDP is connectionless, so it should start immediately
        simulator.IsRunning.Should().BeTrue();
        
        // Cleanup
        await simulator.StopAsync();
    }

    [Fact]
    public async Task OmronFINSSimulator_Should_Support_UDP_Start()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act & Assert
        var startTask = simulator.StartUdpAsync(6001);
        startTask.Should().NotBeNull();
        
        // UDP is connectionless, so it should start immediately
        simulator.IsRunning.Should().BeTrue();
        
        // Cleanup
        await simulator.StopAsync();
    }

    [Fact]
    public async Task MitsubishiMCSimulator_Should_Support_Both_TCP_UDP_Start()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act & Assert
        var startTask = simulator.StartBothAsync(5002, 6002);
        startTask.Should().NotBeNull();
        
        simulator.IsRunning.Should().BeTrue();
        
        // Cleanup
        await simulator.StopAsync();
    }

    [Fact]
    public async Task OmronFINSSimulator_Should_Support_Both_TCP_UDP_Start()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act & Assert
        var startTask = simulator.StartBothAsync(5003, 6003);
        startTask.Should().NotBeNull();
        
        simulator.IsRunning.Should().BeTrue();
        
        // Cleanup
        await simulator.StopAsync();
    }

    [Fact]
    public async Task MitsubishiMCProtocol_Should_Support_UDP_Connection()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol();

        // Act & Assert
        var connectTask = protocol.ConnectUdpAsync("127.0.0.1", 6004);
        connectTask.Should().NotBeNull();
        
        // UDP connection should succeed immediately
        var result = await connectTask;
        result.Should().BeTrue();
        
        protocol.IsConnected.Should().BeTrue();
        
        // Cleanup
        await protocol.DisconnectAsync();
    }

    [Fact]
    public async Task OmronFINSProtocol_Should_Support_UDP_Connection()
    {
        // Arrange
        var protocol = new OmronFINSProtocol();

        // Act & Assert
        var connectTask = protocol.ConnectUdpAsync("127.0.0.1", 6005);
        connectTask.Should().NotBeNull();
        
        // UDP connection should succeed immediately
        var result = await connectTask;
        result.Should().BeTrue();
        
        protocol.IsConnected.Should().BeTrue();
        
        // Cleanup
        await protocol.DisconnectAsync();
    }

    [Fact]
    public async Task MitsubishiMCSimulator_Should_Stop_UDP_Listener_Properly()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act
        await simulator.StartUdpAsync(6006);
        simulator.IsRunning.Should().BeTrue();

        await simulator.StopAsync();

        // Assert
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task OmronFINSSimulator_Should_Stop_UDP_Listener_Properly()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act
        await simulator.StartUdpAsync(6007);
        simulator.IsRunning.Should().BeTrue();

        await simulator.StopAsync();

        // Assert
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Simulator_Should_Handle_Multiple_UDP_Starts_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act - Start UDP multiple times
        await simulator.StartUdpAsync(6008);
        await simulator.StartUdpAsync(6008); // Same port
        
        // Assert - Should still be running
        simulator.IsRunning.Should().BeTrue();
        
        // Cleanup
        await simulator.StopAsync();
    }

    [Fact]
    public async Task Simulator_Should_Handle_Both_TCP_UDP_Stop_Properly()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act
        await simulator.StartBothAsync(5009, 6009);
        simulator.IsRunning.Should().BeTrue();

        await simulator.StopAsync();

        // Assert
        simulator.IsRunning.Should().BeFalse();
    }
}

public class UDPPacketCommunicationTests
{
    [Fact]
    public async Task MitsubishiMCSimulator_Should_Handle_UDP_Packets()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(6010);
        
        using var udpClient = new System.Net.Sockets.UdpClient();
        
        try
        {
            // Act - Send a simple UDP packet to the simulator
            var testPacket = new byte[] { 0x50, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04 };
            await udpClient.SendAsync(testPacket, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6010));
            
            // Wait a short time for processing
            await Task.Delay(100);
            
            // Assert - Simulator should still be running
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            udpClient.Close();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task OmronFINSSimulator_Should_Handle_UDP_Packets()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        await simulator.StartUdpAsync(6011);
        
        using var udpClient = new System.Net.Sockets.UdpClient();
        
        try
        {
            // Act - Send a simple UDP FINS packet to the simulator
            var testPacket = new byte[] 
            { 
                0x80, 0x00, 0x02, // ICF, RSV, GCT
                0x00, 0x00, 0x00, // DNA, DA1, DA2
                0x01, 0x00, 0x00, // SNA, SA1, SA2
                0x00,             // SID
                0x01, 0x01        // Memory area read command
            };
            await udpClient.SendAsync(testPacket, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6011));
            
            // Wait a short time for processing
            await Task.Delay(100);
            
            // Assert - Simulator should still be running
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            udpClient.Close();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task UDP_Simulator_Should_Handle_Multiple_Concurrent_Packets()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(6012);
        
        var tasks = new List<Task>();
        
        try
        {
            // Act - Send multiple packets concurrently
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var udpClient = new System.Net.Sockets.UdpClient();
                    var testPacket = new byte[] { 0x50, 0x00, 0x00, 0x00, (byte)i };
                    await udpClient.SendAsync(testPacket, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6012));
                    udpClient.Close();
                }));
            }
            
            await Task.WhenAll(tasks);
            await Task.Delay(200); // Allow processing time
            
            // Assert - Simulator should still be running
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task UDP_Simulator_Should_Set_And_Get_Device_Values_During_Operation()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(6013);
        
        var address = new PLCAddress("D", 200, 1);
        var testValue = BitConverter.GetBytes((short)9999);
        
        try
        {
            // Act
            simulator.SetDeviceValue(address, testValue);
            var retrievedValue = simulator.GetDeviceValue(address);
            
            // Assert
            retrievedValue.Should().BeEquivalentTo(testValue);
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task UDP_Simulator_Should_Handle_Invalid_Packets_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(6014);
        
        using var udpClient = new System.Net.Sockets.UdpClient();
        
        try
        {
            // Act - Send invalid packets
            var invalidPackets = new[]
            {
                new byte[] { }, // Empty packet
                new byte[] { 0xFF }, // Single byte
                new byte[] { 0x00, 0x00 }, // Too short
                new byte[1000] // Too long
            };
            
            foreach (var packet in invalidPackets)
            {
                await udpClient.SendAsync(packet, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6014));
            }
            
            await Task.Delay(200);
            
            // Assert - Simulator should still be running despite invalid packets
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            udpClient.Close();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Both_TCP_UDP_Simulators_Should_Run_Independently()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartBothAsync(5015, 6015);
        
        using var udpClient = new System.Net.Sockets.UdpClient();
        using var tcpClient = new System.Net.Sockets.TcpClient();
        
        try
        {
            // Act - Test UDP communication
            var udpPacket = new byte[] { 0x50, 0x00, 0x00, 0x00, 0x99 };
            await udpClient.SendAsync(udpPacket, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 6015));
            
            // Test TCP connection (just connection, no data)
            await tcpClient.ConnectAsync(System.Net.IPAddress.Loopback, 5015);
            
            await Task.Delay(200);
            
            // Assert
            simulator.IsRunning.Should().BeTrue();
            tcpClient.Connected.Should().BeTrue();
        }
        finally
        {
            tcpClient.Close();
            udpClient.Close();
            await simulator.StopAsync();
        }
    }
}

/// <summary>
/// 三菱MCプロトコル拡張デバイステスト
/// </summary>
public class MitsubishiMCExtendedDeviceTests
{
    [Theory]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "X")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "Y")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "M")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SM")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "L")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "F")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "C")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "B")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SB")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "S")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "TS")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "TC")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SS")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SC")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "CS")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "CC")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "TN")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SN")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "CN")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "D")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SD")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "W")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "SW")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "Z")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "R")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "ZR")]
    [InlineData(MitsubishiPLCSeries.QJ71E71_Binary_Station1, "ZZR")]
    public void QLiQR_Series_Should_Support_All_Specified_Devices(MitsubishiPLCSeries series, string deviceType)
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(series);

        // Act & Assert
        protocol.IsDeviceSupported(deviceType).Should().BeTrue($"{deviceType} should be supported in {series}");
    }

    [Theory]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "X")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "Y")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "M")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "SM")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "D")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "SD")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "C")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "S")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "TN")]
    [InlineData(MitsubishiPLCSeries.FX5U_CPU_Binary, "CN")]
    public void FX5U_Series_Should_Support_Basic_Devices(MitsubishiPLCSeries series, string deviceType)
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(series);

        // Act & Assert
        protocol.IsDeviceSupported(deviceType).Should().BeTrue($"{deviceType} should be supported in {series}");
    }

    [Theory]
    [InlineData(MitsubishiPLCSeries.FX3U_ENET, "ZZR")] // FXシリーズでサポートされていないデバイス
    [InlineData(MitsubishiPLCSeries.FX3U_ENET, "ZR")]  // FXシリーズでサポートされていないデバイス
    [InlineData(MitsubishiPLCSeries.FX3U_ENET, "R")]   // FXシリーズでサポートされていないデバイス
    public void FX_Series_Should_Not_Support_Advanced_Devices(MitsubishiPLCSeries series, string deviceType)
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(series);

        // Act & Assert
        protocol.IsDeviceSupported(deviceType).Should().BeFalse($"{deviceType} should NOT be supported in {series}");
    }

    [Theory]
    [InlineData("ZZR")] // 不明なデバイス
    [InlineData("UNKNOWN")] // 不明なデバイス
    [InlineData("XX")] // 不明なデバイス
    public void Should_Throw_NotSupportedException_For_Unsupported_Devices(string deviceType)
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.FX3U_ENET); // 基本機能のみのシリーズ
        var address = new PLCAddress(deviceType, 0, 1);

        // Act & Assert
        var readAction = () => protocol.ReadAsync(address);
        var writeAction = () => protocol.WriteAsync(address, new byte[] { 0x00, 0x00 });

        readAction.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*{deviceType}*サポートされていません*");

        writeAction.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*{deviceType}*サポートされていません*");
    }

    [Fact]
    public void Should_Validate_Device_Access_Parameters()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol();

        // Act & Assert - 無効なアドレス
        var invalidAddressAction = () => protocol.ReadAsync(new PLCAddress("D", -1, 1));
        invalidAddressAction.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*アドレスは0以上*");

        // Act & Assert - 無効なサイズ
        var invalidSizeAction = () => protocol.ReadAsync(new PLCAddress("D", 0, 0));
        invalidSizeAction.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*サイズは1以上*");

        // Act & Assert - 空のデバイスタイプ
        var emptyDeviceAction = () => protocol.ReadAsync(new PLCAddress("", 0, 1));
        emptyDeviceAction.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*デバイスタイプが指定されていません*");
    }

    [Fact]
    public void Simulator_Should_Return_Error_For_Unsupported_Device_Codes()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.FX3U_ENET);

        // Act & Assert - 基本シリーズでサポートされていないデバイスコードをチェック
        simulator.IsDeviceCodeSupported(0xB1).Should().BeFalse(); // ZZR (未サポート)
        simulator.IsDeviceCodeSupported(0xB0).Should().BeFalse(); // ZR (未サポート)
        simulator.IsDeviceCodeSupported(0xAF).Should().BeFalse(); // R (未サポート)

        // サポートされているデバイスコードをチェック
        simulator.IsDeviceCodeSupported(0xA8).Should().BeTrue(); // D (サポート)
        simulator.IsDeviceCodeSupported(0x9C).Should().BeTrue(); // X (サポート)
        simulator.IsDeviceCodeSupported(0x90).Should().BeTrue(); // M (サポート)
    }

    [Fact]
    public void Should_Get_Supported_Device_List()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1);

        // Act
        var protocolDevices = protocol.GetSupportedDevices();
        var simulatorDevices = simulator.GetSupportedDevices();

        // Assert
        protocolDevices.Should().NotBeEmpty();
        simulatorDevices.Should().NotBeEmpty();
        
        // Q/L/iQ-Rシリーズは全デバイスをサポート
        protocolDevices.Should().ContainKeys("X", "Y", "M", "SM", "L", "F", "C", "B", "SB", 
                                           "S", "TS", "TC", "SS", "SC", "CS", "CC",
                                           "TN", "SN", "CN", "D", "SD", "W", "SW", 
                                           "Z", "R", "ZR", "ZZR");

        // プロトコルとシミュレータで同じデバイス情報を返すことを確認
        protocolDevices.Should().BeEquivalentTo(simulatorDevices);
    }

    [Fact]
    public void Should_Get_Series_Description()
    {
        // Arrange
        var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1);

        // Act
        var description = simulator.GetSeriesDescription();

        // Assert
        description.Should().NotBeNullOrEmpty();
        description.Should().Contain("Q/L/iQ-R");
    }
}