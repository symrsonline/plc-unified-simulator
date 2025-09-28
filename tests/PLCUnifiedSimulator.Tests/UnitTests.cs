using FluentAssertions;
using PLCUnifiedSimulator.Core;
using PLCUnifiedSimulator.Protocols.Mitsubishi;
using PLCUnifiedSimulator.Protocols.Omron;
using PLCUnifiedSimulator.Simulators;
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