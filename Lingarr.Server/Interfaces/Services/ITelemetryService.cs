using Lingarr.Server.Models.Telemetry;

namespace Lingarr.Server.Interfaces.Services;

public interface ITelemetryService
{
    Task<TelemetryPayload> GenerateTelemetryPayload();
    Task<bool> CanSubmitTelemetry();
    Task<bool> SubmitTelemetry(TelemetryPayload payload);
}
