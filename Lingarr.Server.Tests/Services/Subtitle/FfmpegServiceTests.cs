using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Services.Subtitle;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Lingarr.Server.Tests.Services.Subtitle;

public class FfmpegServiceTests : IDisposable
{
    private readonly string _directory;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly FfmpegService _service;

    public FfmpegServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"lingarr-ffmpeg-tests-{Guid.NewGuid():N}");
        _httpClientFactoryMock
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_httpMessageHandlerMock.Object));

        _service = new FfmpegService(
            _httpClientFactoryMock.Object,
            new Mock<IProcessRunner>().Object,
            NullLogger<FfmpegService>.Instance,
            _directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void IsInstalled_WithoutPrograms_ReturnsFalse()
    {
        Assert.False(_service.IsInstalled());
    }

    [Fact]
    public void IsInstalled_WithBothPrograms_ReturnsTrue()
    {
        WritePrograms();

        Assert.True(_service.IsInstalled());
    }

    [Fact]
    public async Task Install_WithProgramsCopiedByUser_DoesNotDownload()
    {
        WritePrograms();

        await _service.Install(CancellationToken.None);

        _httpClientFactoryMock.Verify(factory => factory.CreateClient(It.IsAny<string>()), Times.Never);
        Assert.False(File.Exists(Path.Combine(_directory, "version")));
    }

    [Fact]
    public async Task Install_WhenChecksumDoesNotMatch_RemovesDownload()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), "The ffmpeg download exists only for Linux.");
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3])
            });

        await Assert.ThrowsAsync<InvalidDataException>(() => _service.Install(CancellationToken.None));

        Assert.False(_service.IsInstalled());
        Assert.Empty(Directory.GetFiles(_directory));
    }

    private void WritePrograms()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "ffmpeg"), "");
        File.WriteAllText(Path.Combine(_directory, "ffprobe"), "");
    }
}
