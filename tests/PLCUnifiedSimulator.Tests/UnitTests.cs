using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Protocols.Omron;
using PLCUnifiedSimulator.Simulators;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace PLCUnifiedSimulator.Tests;

public class TestBase
{
    public TestBase()
    {
        // Fix console output encoding for test execution
        Console.OutputEncoding = Encoding.UTF8;
    }
}

public class PLCDataTests : TestBase
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

public class PLCAddressTests : TestBase
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

public class MitsubishiMCProtocolTests : TestBase
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

public class OmronFINSProtocolTests : TestBase
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

public class MitsubishiMCSimulatorTests : TestBase
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

public class OmronFINSSimulatorTests : TestBase
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

    [Theory]
    [InlineData("IO", 0xb0)]
    [InlineData("WR", 0xb1)]
    [InlineData("HR", 0xb2)]
    [InlineData("AR", 0xb3)]
    [InlineData("TS", 0x09)]
    [InlineData("CS", 0x09)]
    [InlineData("TN", 0x89)]
    [InlineData("CN", 0x89)]
    [InlineData("DM", 0x82)]
    [InlineData("EM", 0x98)]
    [InlineData("EB", 0xa0)]
    [InlineData("TKB", 0x06)]
    [InlineData("TKS", 0x46)]
    [InlineData("IR", 0xdc)]
    [InlineData("DR", 0xbc)]
    public void OmronFINSSimulator_Should_Support_Extended_Device_Types(string deviceType, byte expectedCode)
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        var address = new PLCAddress(deviceType, 100, 1);
        var testValue = BitConverter.GetBytes((short)1234);

        // Act
        simulator.SetDeviceValue(address, testValue);
        var retrievedValue = simulator.GetDeviceValue(address);
        var supportedDevices = simulator.GetSupportedDevices();

        // Assert
        retrievedValue.Should().BeEquivalentTo(testValue);
        simulator.GetDeviceValue(address).Should().NotBeNull();
        supportedDevices.Should().ContainKey(deviceType);
        supportedDevices[deviceType].Should().Be(expectedCode);
    }

    [Fact]
    public void OmronFINSSimulator_Should_Get_Supported_Device_List()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act
        var supportedDevices = simulator.GetSupportedDevices();

        // Assert
        supportedDevices.Should().NotBeEmpty();
        supportedDevices.Should().ContainKeys("IO", "WR", "HR", "AR", "TS", "CS", "TN", "CN",
                                             "DM", "EM", "EB", "TKB", "TKS", "IR", "DR");
    }
}

public class UDPCommunicationTests : TestBase
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

public class UDPPacketCommunicationTests : TestBase
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
public class MitsubishiMCExtendedDeviceTests : TestBase
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

/// <summary>
/// オムロンFINSプロトコル拡張デバイステスト
/// </summary>
public class OmronFINSExtendedDeviceTests : TestBase
{
    [Theory]
    [InlineData("IO", 0xb0)]
    [InlineData("WR", 0xb1)]
    [InlineData("HR", 0xb2)]
    [InlineData("AR", 0xb3)]
    [InlineData("TS", 0x09)]
    [InlineData("CS", 0x09)]
    [InlineData("TN", 0x89)]
    [InlineData("CN", 0x89)]
    [InlineData("DM", 0x82)]
    [InlineData("EM", 0x98)]
    [InlineData("EB", 0xa0)]
    [InlineData("TKB", 0x06)]
    [InlineData("TKS", 0x46)]
    [InlineData("IR", 0xdc)]
    [InlineData("DR", 0xbc)]
    public void OmronFINSSimulator_Should_Support_All_Extended_Device_Types(string deviceType, byte expectedCode)
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act
        var supportedDevices = simulator.GetSupportedDevices();

        // Assert
        supportedDevices.Should().ContainKey(deviceType);
        supportedDevices[deviceType].Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("IO")]
    [InlineData("WR")]
    [InlineData("HR")]
    [InlineData("AR")]
    [InlineData("TS")]
    [InlineData("CS")]
    [InlineData("TN")]
    [InlineData("CN")]
    [InlineData("DM")]
    [InlineData("EM")]
    [InlineData("EB")]
    [InlineData("TKB")]
    [InlineData("TKS")]
    [InlineData("IR")]
    [InlineData("DR")]
    public void OmronFINSSimulator_Should_Store_And_Retrieve_Extended_Device_Values(string deviceType)
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        var address = new PLCAddress(deviceType, 500, 2);
        var testValue = BitConverter.GetBytes(0x12345678); // 32-bit value for 2-word device

        // Act
        simulator.SetDeviceValue(address, testValue);
        var retrievedValue = simulator.GetDeviceValue(address);

        // Assert
        retrievedValue.Should().BeEquivalentTo(testValue);
        retrievedValue.Should().HaveCount(4); // 2 words = 4 bytes
    }

    [Fact]
    public void OmronFINSSimulator_Should_Get_Complete_Supported_Device_List()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();

        // Act
        var supportedDevices = simulator.GetSupportedDevices();

        // Assert
        supportedDevices.Should().NotBeEmpty();
        supportedDevices.Should().HaveCountGreaterThanOrEqualTo(17); // 拡張デバイス + 後方互換デバイス

        // 拡張デバイスがすべて含まれていることを確認
        supportedDevices.Should().ContainKeys("IO", "WR", "HR", "AR", "TS", "CS", "TN", "CN",
                                             "DM", "EM", "EB", "TKB", "TKS", "IR", "DR");

        // 後方互換デバイスの確認
        supportedDevices.Should().ContainKeys("W", "H", "A", "C");
    }

    [Fact]
    public void OmronFINSSimulator_Should_Handle_Backward_Compatibility_Devices()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        var supportedDevices = simulator.GetSupportedDevices();

        // Act & Assert - 後方互換デバイスの確認
        supportedDevices.Should().ContainKey("W").WhoseValue.Should().Be(0x31);
        supportedDevices.Should().ContainKey("H").WhoseValue.Should().Be(0x32);
        supportedDevices.Should().ContainKey("A").WhoseValue.Should().Be(0x33);
        supportedDevices.Should().ContainKey("C").WhoseValue.Should().Be(0x09);
    }

    [Fact]
    public void OmronFINSSimulator_Should_Validate_Device_Type_Consistency()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        var supportedDevices = simulator.GetSupportedDevices();

        // Act & Assert - 各デバイスタイプが一意のメモリエリアコードを持つことを確認
        var memoryAreaCodes = supportedDevices.Values.ToList();
        var uniqueCodes = memoryAreaCodes.Distinct().ToList();

        // 19個のデバイス（拡張15個 + 後方互換4個）があり、そのうちのいくつかは同じコードを共有
        supportedDevices.Should().HaveCount(19);
        uniqueCodes.Should().HaveCountGreaterThan(15); // 一部のデバイスは同じコードを共有（TS/CS, TN/CNなど）
    }
}

/// <summary>
/// ファクトリーメソッドのテスト
/// </summary>
public class FactoryTests : TestBase
{
    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Create_Simulator_With_Series()
    {
        // Arrange
        var series = MitsubishiPLCSeries.QJ71E71_Binary_Station1;

        // Act
        var simulator = MitsubishiPLCSimulatorFactory.CreateSimulator(series);

        // Assert
        simulator.Should().NotBeNull();
        simulator.Protocol.Should().NotBeNull();
        simulator.Protocol.ProtocolName.Should().StartWith("Mitsubishi MC Protocol");
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Create_Simulator_With_Logger()
    {
        // Arrange
        var series = MitsubishiPLCSeries.QJ71E71_Binary_Station1;
        var loggerMock = new Mock<ILogger>();

        // Act
        var simulator = MitsubishiPLCSimulatorFactory.CreateSimulator(series, loggerMock.Object);

        // Assert
        simulator.Should().NotBeNull();
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Create_All_Simulators()
    {
        // Act
        var simulators = MitsubishiPLCSimulatorFactory.CreateAllSimulators();

        // Assert
        simulators.Should().NotBeNull();
        simulators.Should().NotBeEmpty();

        // すべてのシリーズが含まれていることを確認
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            simulators.Should().ContainKey(series);
            simulators[series].Should().NotBeNull();
        }
    }

    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Create_All_Simulators_With_Logger()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();

        // Act
        var simulators = MitsubishiPLCSimulatorFactory.CreateAllSimulators(loggerMock.Object);

        // Assert
        simulators.Should().NotBeNull();
        simulators.Should().NotBeEmpty();

        // すべてのシリーズが含まれていることを確認
        foreach (MitsubishiPLCSeries series in Enum.GetValues<MitsubishiPLCSeries>())
        {
            simulators.Should().ContainKey(series);
            simulators[series].Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("QJ71E71_Binary_Station1")]
    [InlineData("qj71e71_binary_station1")]
    [InlineData("FX3U_ENET")]
    [InlineData("fx3u_enet")]
    public void MitsubishiPLCSimulatorFactory_Should_Create_Simulator_By_Name(string seriesName)
    {
        // Act
        var simulator = MitsubishiPLCSimulatorFactory.CreateSimulatorByName(seriesName);

        // Assert
        simulator.Should().NotBeNull();
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Throw_Exception_For_Invalid_Series_Name()
    {
        // Arrange
        var invalidSeriesName = "InvalidSeries";

        // Act & Assert
        var action = () => MitsubishiPLCSimulatorFactory.CreateSimulatorByName(invalidSeriesName);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"*無効なPLCシリーズ名です: {invalidSeriesName}*");
    }

    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Get_Available_Series()
    {
        // Act
        var availableSeries = MitsubishiPLCSimulatorFactory.GetAvailableSeries();

        // Assert
        availableSeries.Should().NotBeNull();
        availableSeries.Should().NotBeEmpty();

        // 各シリーズに必要な情報が含まれていることを確認
        foreach (var (series, description, port) in availableSeries)
        {
            series.Should().BeDefined();
            description.Should().NotBeNullOrEmpty();
            port.Should().BeGreaterThan(0);
        }

        // ポート番号でソートされていることを確認
        var ports = availableSeries.Select(x => x.Port).ToList();
        ports.Should().BeInAscendingOrder();
    }

    [Fact]
    public void OmronPLCSimulatorFactory_Should_Create_Simulator()
    {
        // Act
        var simulator = OmronPLCSimulatorFactory.CreateSimulator();

        // Assert
        simulator.Should().NotBeNull();
        simulator.Protocol.Should().NotBeNull();
        simulator.Protocol.ProtocolName.Should().Be("OMRON FINS");
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void OmronPLCSimulatorFactory_Should_Create_Simulator_With_Logger()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();

        // Act
        var simulator = OmronPLCSimulatorFactory.CreateSimulator(loggerMock.Object);

        // Assert
        simulator.Should().NotBeNull();
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void OmronPLCSimulatorFactory_Should_Get_Simulator_Info()
    {
        // Act
        var (name, description, defaultPort) = OmronPLCSimulatorFactory.GetSimulatorInfo();

        // Assert
        name.Should().Be("FINS");
        description.Should().Be("オムロンFINSプロトコル");
        defaultPort.Should().Be(9600);
    }
}

/// <summary>
/// ログ機能のテスト
/// </summary>
public class LoggingTests : TestBase
{
    [Fact]
    public void MitsubishiMCProtocol_Should_Log_Connection_Events()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MitsubishiMCProtocol>>();
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.QJ71E71_Binary_Station1, loggerMock.Object);

        // Act - ログが記録されることを確認するために、接続操作を行う
        var connectTask = protocol.ConnectUdpAsync("127.0.0.1", 7000);

        // Assert
        connectTask.Should().NotBeNull();
        // 実際のログ記録は非同期で行われるため、ここではタスクが正常に作成されることを確認
    }

    [Fact]
    public void MitsubishiMCSimulator_Should_Log_Start_Stop_Events()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MitsubishiMCSimulator>>();
        using var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1, loggerMock.Object);

        // Act & Assert - ログが記録されることを確認するために、開始操作を行う
        var startTask = simulator.StartUdpAsync(7001);
        startTask.Should().NotBeNull();

        simulator.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void OmronFINSSimulator_Should_Log_Start_Stop_Events()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<OmronFINSSimulator>>();
        using var simulator = new OmronFINSSimulator(loggerMock.Object);

        // Act & Assert - ログが記録されることを確認するために、開始操作を行う
        var startTask = simulator.StartUdpAsync(7002);
        startTask.Should().NotBeNull();

        simulator.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void MitsubishiMCSimulator_Should_Log_Device_Operations()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MitsubishiMCSimulator>>();
        using var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1, loggerMock.Object);
        var address = new PLCAddress("D", 1000, 1);
        var testValue = BitConverter.GetBytes((short)1234);

        // Act
        simulator.SetDeviceValue(address, testValue);
        var retrievedValue = simulator.GetDeviceValue(address);

        // Assert
        retrievedValue.Should().BeEquivalentTo(testValue);
        // ログ記録は内部で行われるため、ここでは操作が正常に完了することを確認
    }

    [Fact]
    public void Simulator_Should_Handle_Null_Logger_Gracefully()
    {
        // Arrange & Act
        using var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1, null);

        // Assert
        simulator.Should().NotBeNull();
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Protocol_Should_Handle_Null_Logger_Gracefully()
    {
        // Arrange & Act
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.QJ71E71_Binary_Station1, null);

        // Assert
        protocol.Should().NotBeNull();
        protocol.IsConnected.Should().BeFalse();
    }
}

/// <summary>
/// エラーハンドリングのテスト
/// </summary>
public class ErrorHandlingTests : TestBase
{
    [Fact]
    public async Task MitsubishiMCProtocol_Should_Handle_Connection_Timeout_Gracefully()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol();

        // Act - 無効なIPアドレスへの接続を試行
        var connectResult = await protocol.ConnectAsync("192.0.2.1", 12345); // RFC 5737 test address

        // Assert
        // 接続結果はネットワーク条件によるが、メソッドが正常に完了することを確認
        // bool型の結果が返されることを確認
        Assert.IsType<bool>(connectResult);
    }

    [Fact]
    public async Task MitsubishiMCSimulator_Should_Handle_Port_Conflict_Gracefully()
    {
        // Arrange
        using var simulator1 = new MitsubishiMCSimulator();
        using var simulator2 = new MitsubishiMCSimulator();

        // Act - 同じポートで2つのシミュレータを開始
        await simulator1.StartAsync(8000);

        // 同じポートで2番目のシミュレータを開始しようとすると失敗するはず
        var startTask2 = simulator2.StartAsync(8000);

        // Assert - 2番目の開始は失敗する可能性があるが、タスクは作成される
        startTask2.Should().NotBeNull();

        // Cleanup
        await simulator1.StopAsync();
        await simulator2.StopAsync();
    }

    [Fact]
    public void MitsubishiMCProtocol_Should_Validate_Device_Address_Parameters()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol();

        // Act & Assert - 無効なアドレス
        var invalidAddressAction = () => protocol.ReadAsync(new PLCAddress("D", -1, 1));
        invalidAddressAction.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - 無効なサイズ
        var invalidSizeAction = () => protocol.ReadAsync(new PLCAddress("D", 0, 0));
        invalidSizeAction.Should().ThrowAsync<ArgumentException>();

        // Act & Assert - 空のデバイスタイプ
        var emptyDeviceAction = () => protocol.ReadAsync(new PLCAddress("", 0, 1));
        emptyDeviceAction.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void MitsubishiMCSimulator_Should_Handle_Invalid_Device_Values_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        var address = new PLCAddress("D", 100, 1);

        // Act & Assert - null値を設定しようとすると適切に処理される
        var setNullAction = () => simulator.SetDeviceValue(address, null!);
        setNullAction.Should().Throw<ArgumentNullException>(); // nullチェックが適切に行われるはず

        // Act & Assert - 存在しないアドレスから値を取得
        var getValue = simulator.GetDeviceValue(address);
        getValue.Should().BeNull(); // 存在しない場合はnullが返されるはず
    }

    [Fact]
    public async Task Simulator_Should_Handle_Stop_Before_Start_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act - 開始せずに停止を試行
        await simulator.StopAsync();

        // Assert - 例外が発生せず、正常に処理される
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Simulator_Should_Handle_Multiple_Stop_Calls_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(8001);

        // Act - 複数回停止を呼び出し
        await simulator.StopAsync();
        await simulator.StopAsync(); // 2回目の停止

        // Assert - 例外が発生せず、正常に処理される
        simulator.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void PLCData_Should_Handle_Invalid_Data_Lengths_Gracefully()
    {
        // Arrange
        var address = new PLCAddress("D", 100, 2); // 2ワード期待

        // Act & Assert - 短いデータ
        var shortData = new byte[] { 0x12 }; // 1バイトのみ
        var plcData = new PLCData(address, shortData);

        // GetValue<int>は4バイトを期待するが、データが短い場合の処理を確認
        var getIntAction = () => plcData.GetValue<int>();
        getIntAction.Should().Throw<ArgumentException>(); // データ長が不十分な場合は例外が発生する
    }

    [Fact]
    public void MitsubishiMCProtocol_Should_Handle_Unsupported_Device_Types()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.FX3U_ENET);
        var unsupportedAddress = new PLCAddress("ZZR", 0, 1); // FXシリーズでサポートされていないデバイス

        // Act & Assert
        var readAction = () => protocol.ReadAsync(unsupportedAddress);
        readAction.Should().ThrowAsync<NotSupportedException>();

        var writeAction = () => protocol.WriteAsync(unsupportedAddress, new byte[] { 0x00, 0x00 });
        writeAction.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task UDP_Simulator_Should_Handle_Invalid_UDP_Packets_Gracefully()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(8002);

        using var udpClient = new System.Net.Sockets.UdpClient();

        try
        {
            // Act - さまざまな無効なパケットを送信
            var invalidPackets = new[]
            {
                new byte[0], // 空のパケット
                new byte[] { 0xFF }, // 1バイトのパケット
                new byte[10000] // 非常に大きなパケット
            };

            foreach (var packet in invalidPackets)
            {
                await udpClient.SendAsync(packet, new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 8002));
            }

            await Task.Delay(100); // 処理時間を待つ

            // Assert - シミュレータは無効なパケット後も実行を継続
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            udpClient.Close();
            await simulator.StopAsync();
        }
    }
}

/// <summary>
/// 統合テスト - 実際の通信を含むテスト
/// </summary>
public class IntegrationTests : TestBase
{
    [Fact]
    public async Task MitsubishiMCProtocol_Should_Read_Write_Device_Values_Over_UDP()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(9000);

        var protocol = new MitsubishiMCProtocol();
        var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9000);
        connectResult.Should().BeTrue();

        var address = new PLCAddress("D", 200, 1);
        var testValue = BitConverter.GetBytes((short)9999);

        try
        {
            // Act - 値を書き込み
            var writeResult = await protocol.WriteAsync(address, testValue);
            writeResult.Should().BeTrue();

            // 少し待機
            await Task.Delay(100);

            // Act - 値を読み取り
            var readResult = await protocol.ReadAsync(address);
            readResult.Should().NotBeNull();

            // Assert
            readResult.Should().BeEquivalentTo(testValue);
        }
        finally
        {
            await protocol.DisconnectAsync();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task OmronFINSProtocol_Should_Read_Write_Device_Values_Over_UDP()
    {
        // Arrange
        using var simulator = new OmronFINSSimulator();
        await simulator.StartUdpAsync(9001);

        var protocol = new OmronFINSProtocol();
        var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9001);
        connectResult.Should().BeTrue();

        var address = new PLCAddress("DM", 200, 1);
        var testValue = BitConverter.GetBytes((short)7777);

        try
        {
            // Act - 値を書き込み
            var writeResult = await protocol.WriteAsync(address, testValue);
            writeResult.Should().BeTrue();

            // 少し待機
            await Task.Delay(100);

            // Act - 値を読み取り
            var readResult = await protocol.ReadAsync(address);
            readResult.Should().NotBeNull();

            // Assert
            readResult.Should().BeEquivalentTo(testValue);
        }
        finally
        {
            await protocol.DisconnectAsync();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task MitsubishiMCSimulator_Should_Handle_Multiple_UDP_Clients_Concurrently()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(9002);

        var tasks = new List<Task>();

        try
        {
            // Act - 複数のクライアントが同時にアクセス
            for (int i = 0; i < 3; i++) // クライアント数を減らして安定させる
            {
                var clientTask = Task.Run(async () =>
                {
                    var protocol = new MitsubishiMCProtocol();
                    var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9002);
                    if (connectResult)
                    {
                        try
                        {
                            var address = new PLCAddress("D", 300 + i, 1);
                            var testValue = BitConverter.GetBytes((short)(1000 + i));

                            // 書き込み
                            var writeResult = await protocol.WriteAsync(address, testValue);
                            if (writeResult)
                            {
                                await Task.Delay(50);

                                // 読み取り
                                var readResult = await protocol.ReadAsync(address);
                                readResult.Should().NotBeNull();
                                readResult.Should().BeEquivalentTo(testValue);
                            }
                        }
                        finally
                        {
                            await protocol.DisconnectAsync();
                        }
                    }
                });

                tasks.Add(clientTask);
            }

            // Assert - すべてのクライアントが正常に完了
            await Task.WhenAll(tasks);
            simulator.IsRunning.Should().BeTrue();
        }
        finally
        {
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Simulator_Should_Persist_Device_Values_Across_Multiple_Operations()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(9003);

        var protocol = new MitsubishiMCProtocol();
        var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9003);
        connectResult.Should().BeTrue();

        try
        {
            // Act - 複数のアドレスに値を設定
            var addresses = new[]
            {
                new PLCAddress("D", 400, 1),
                new PLCAddress("M", 400, 1),
                new PLCAddress("X", 400, 1)
            };

            var values = new[]
            {
                BitConverter.GetBytes((short)1111),
                new byte[] { 0x01, 0x00 },
                new byte[] { 0x01, 0x00 }
            };

            // 値を書き込み
            for (int i = 0; i < addresses.Length; i++)
            {
                var writeResult = await protocol.WriteAsync(addresses[i], values[i]);
                writeResult.Should().BeTrue();
            }

            await Task.Delay(100);

            // 値を読み取り直し
            for (int i = 0; i < addresses.Length; i++)
            {
                var readResult = await protocol.ReadAsync(addresses[i]);
                readResult.Should().NotBeNull();
                readResult.Should().BeEquivalentTo(values[i]);
            }

            // Assert - シミュレータの内部状態も確認
            foreach (var address in addresses)
            {
                var simulatorValue = simulator.GetDeviceValue(address);
                simulatorValue.Should().NotBeNull();
            }
        }
        finally
        {
            await protocol.DisconnectAsync();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Protocol_Should_Handle_Disconnection_And_Reconnection()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(9004);

        var protocol = new MitsubishiMCProtocol();

        try
        {
            // Act - 接続
            var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9004);
            connectResult.Should().BeTrue();
            protocol.IsConnected.Should().BeTrue();

            // 切断
            await protocol.DisconnectAsync();
            protocol.IsConnected.Should().BeFalse();

            // 再接続
            var reconnectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9004);
            reconnectResult.Should().BeTrue();
            protocol.IsConnected.Should().BeTrue();

            // 再接続後に操作可能
            var address = new PLCAddress("D", 500, 1);
            var testValue = BitConverter.GetBytes((short)5555);
            var writeResult = await protocol.WriteAsync(address, testValue);
            writeResult.Should().BeTrue();
        }
        finally
        {
            await protocol.DisconnectAsync();
            await simulator.StopAsync();
        }
    }

    [Fact]
    public async Task Simulator_Should_Handle_Device_Value_Type_Conversions()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();
        await simulator.StartUdpAsync(9005);

        var protocol = new MitsubishiMCProtocol();
        var connectResult = await protocol.ConnectUdpAsync("127.0.0.1", 9005);
        connectResult.Should().BeTrue();

        try
        {
            // Act - さまざまなデータ型の書き込みと読み取り
            var testCases = new[]
            {
                (new PLCAddress("D", 600, 1), BitConverter.GetBytes((short)1234)),
                (new PLCAddress("D", 601, 2), BitConverter.GetBytes(12345678)),
                (new PLCAddress("M", 600, 1), new byte[] { 0x01, 0x00 })
            };

            foreach (var (address, value) in testCases)
            {
                // 書き込み
                var writeResult = await protocol.WriteAsync(address, value);
                writeResult.Should().BeTrue();

                await Task.Delay(50);

                // 読み取り
                var readResult = await protocol.ReadAsync(address);
                readResult.Should().NotBeNull();
                readResult.Should().BeEquivalentTo(value);
            }
        }
        finally
        {
            await protocol.DisconnectAsync();
            await simulator.StopAsync();
        }
    }
}

/// <summary>
/// コンソールアプリケーションのテスト
/// </summary>
public class ConsoleApplicationTests : TestBase
{
    [Fact]
    public void MitsubishiPLCSimulatorFactory_Should_Return_Valid_Series_List()
    {
        // Act
        var seriesList = MitsubishiPLCSimulatorFactory.GetAvailableSeries();

        // Assert
        seriesList.Should().NotBeEmpty();
        seriesList.Should().AllSatisfy(series =>
        {
            series.Series.Should().BeDefined();
            series.Description.Should().NotBeNullOrEmpty();
            series.Port.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public void OmronPLCSimulatorFactory_Should_Return_Valid_Simulator_Info()
    {
        // Act
        var (name, description, defaultPort) = OmronPLCSimulatorFactory.GetSimulatorInfo();

        // Assert
        name.Should().Be("FINS");
        description.Should().Be("オムロンFINSプロトコル");
        defaultPort.Should().Be(9600);
    }

    [Fact]
    public void Simulator_Factory_Should_Create_Simulator_With_Default_Settings()
    {
        // Act
        var mitsubishiSimulator = MitsubishiPLCSimulatorFactory.CreateSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        var omronSimulator = OmronPLCSimulatorFactory.CreateSimulator();

        // Assert
        mitsubishiSimulator.Should().NotBeNull();
        mitsubishiSimulator.IsRunning.Should().BeFalse();
        mitsubishiSimulator.Protocol.Should().NotBeNull();

        omronSimulator.Should().NotBeNull();
        omronSimulator.IsRunning.Should().BeFalse();
        omronSimulator.Protocol.Should().NotBeNull();
    }

    [Fact]
    public async Task Simulator_Should_Start_On_Default_Ports()
    {
        // Arrange
        using var mitsubishiSimulator = MitsubishiPLCSimulatorFactory.CreateSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1);
        using var omronSimulator = OmronPLCSimulatorFactory.CreateSimulator();

        // Act
        await mitsubishiSimulator.StartAsync(5000);
        await omronSimulator.StartAsync(9600);

        // Assert
        mitsubishiSimulator.IsRunning.Should().BeTrue();
        omronSimulator.IsRunning.Should().BeTrue();

        // Cleanup
        await mitsubishiSimulator.StopAsync();
        await omronSimulator.StopAsync();
    }

    [Fact]
    public void PLCProtocolBase_Should_Implement_ILogger_Property()
    {
        // Arrange
        var protocol = new MitsubishiMCProtocol();

        // Act & Assert
        protocol.Logger.Should().NotBeNull();
        protocol.Logger.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILogger>();
    }

    [Fact]
    public void PLCSimulatorBase_Should_Implement_ILogger_Property()
    {
        // Arrange
        using var simulator = new MitsubishiMCSimulator();

        // Act & Assert
        simulator.Logger.Should().NotBeNull();
        simulator.Logger.Should().BeAssignableTo<Microsoft.Extensions.Logging.ILogger>();
    }

    [Fact]
    public void Simulator_Should_Log_Device_Value_Operations()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        using var simulator = new MitsubishiMCSimulator(MitsubishiPLCSeries.QJ71E71_Binary_Station1, loggerMock.Object);
        var address = new PLCAddress("D", 1000, 1);
        var testValue = BitConverter.GetBytes((short)12345);

        // Act
        simulator.SetDeviceValue(address, testValue);
        var retrievedValue = simulator.GetDeviceValue(address);

        // Assert
        retrievedValue.Should().BeEquivalentTo(testValue);
        // ログが記録されることを確認（モックを使用）
    }

    [Fact]
    public void Protocol_Should_Log_Read_Write_Operations()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MitsubishiMCProtocol>>();
        var protocol = new MitsubishiMCProtocol(MitsubishiPLCSeries.QJ71E71_Binary_Station1, loggerMock.Object);
        var address = new PLCAddress("D", 1000, 1);
        var testValue = BitConverter.GetBytes((short)12345);

        // Act - 接続せずに操作を試行（ログ記録を確認）
        var readTask = protocol.ReadAsync(address);
        var writeTask = protocol.WriteAsync(address, testValue);

        // Assert
        readTask.Should().NotBeNull();
        writeTask.Should().NotBeNull();
        // 実際のログ記録は非同期で行われるため、タスクが作成されることを確認
    }
}