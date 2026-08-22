using System;
using System.Globalization;
using System.Threading.Tasks;
using Lingarr.Core.Configuration;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lingarr.Server.Tests.Services;

public class TelemetryServiceScheduleTests
{
    [Theory]
    [InlineData(6, false)]
    [InlineData(8, true)]
    public async Task CanSubmitTelemetry_MeasuresTheIntervalFromTheStoredInstant(int daysAgo, bool expected)
    {
        var lastSubmission = DateTime.UtcNow.AddDays(-daysAgo).ToString("O");

        Assert.Equal(expected, await BuildService(lastSubmission).CanSubmitTelemetry());
    }

    [Fact]
    public async Task GenerateTelemetryPayload_ReportsAGregorianDate()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("th-TH");
        try
        {
            var payload = await BuildService(string.Empty).GenerateTelemetryPayload();

            // A Buddhist calendar year still matches the receiver's YYYY-MM-DD check, so it is
            // stored as a date roughly 543 years out rather than rejected.
            Assert.Equal(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), payload.ReportDate);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static TelemetryService BuildService(string lastSubmission)
    {
        var statisticsService = new Mock<IStatisticsService>();
        statisticsService.Setup(service => service.GetStatistics()).ReturnsAsync(new Statistics());

        var settingService = new Mock<ISettingService>();
        settingService.Setup(service => service.GetSetting(SettingKeys.Telemetry.TelemetryEnabled))
            .ReturnsAsync("true");
        settingService.Setup(service => service.GetSetting(SettingKeys.Telemetry.LastSubmission))
            .ReturnsAsync(lastSubmission);
        settingService.Setup(service => service.GetSetting(SettingKeys.Telemetry.InstallationId))
            .ReturnsAsync("installation");

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(host => host.EnvironmentName).Returns(Environments.Production);

        return new TelemetryService(
            statisticsService.Object,
            settingService.Object,
            NullLogger<TelemetryService>.Instance,
            new Mock<ILingarrApiService>().Object,
            environment.Object);
    }
}
