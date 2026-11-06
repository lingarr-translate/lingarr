using System;
using System.IO;
using Lingarr.Core.Configuration;
using Xunit;

namespace Lingarr.Server.Tests.Configuration;

[Collection("StartupServiceEnv")]
public class EnvironmentFileConfigurationTests : IDisposable
{
    private const string VariableName = "LINGARR_TEST_SECRET";

    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"lingarr-env-{Guid.NewGuid():N}");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(VariableName, null);
        Environment.SetEnvironmentVariable(VariableName + "_FILE", null);
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Theory]
    [InlineData("value", "value")]
    [InlineData("value\n", "value")]
    [InlineData("value\r\n\n", "value")]
    public void Apply_FileVariableSet_SetsVariableFromFile(string fileContent, string expectedValue)
    {
        // Arrange
        File.WriteAllText(_filePath, fileContent);
        Environment.SetEnvironmentVariable(VariableName + "_FILE", _filePath);

        // Act
        EnvironmentFileConfiguration.Apply();

        // Assert
        Assert.Equal(expectedValue, Environment.GetEnvironmentVariable(VariableName));
    }

    [Fact]
    public void Apply_BothVariablesSet_PrefersFile()
    {
        // Arrange
        File.WriteAllText(_filePath, "from-file");
        Environment.SetEnvironmentVariable(VariableName, "from-env");
        Environment.SetEnvironmentVariable(VariableName + "_FILE", _filePath);

        // Act
        EnvironmentFileConfiguration.Apply();

        // Assert
        Assert.Equal("from-file", Environment.GetEnvironmentVariable(VariableName));
    }

    [Fact]
    public void Apply_FileMissing_KeepsDirectVariable()
    {
        // Arrange
        Environment.SetEnvironmentVariable(VariableName, "from-env");
        Environment.SetEnvironmentVariable(VariableName + "_FILE", _filePath);

        // Act
        EnvironmentFileConfiguration.Apply();

        // Assert
        Assert.Equal("from-env", Environment.GetEnvironmentVariable(VariableName));
    }
}
