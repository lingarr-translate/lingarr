using Lingarr.Server.Models;

namespace Lingarr.Server.Interfaces.Services;

public interface IIntegrationService
{
    /// <summary>
    /// Asynchronously sends an HTTP GET request to the specified API endpoint and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The expected type of the response data.</typeparam>
    /// <param name="apiUrl">The relative API endpoint URL to send the request to.</param>
    /// <param name="settingKeys">The integration settings containing the base URL and API key.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the deserialized response
    /// of type <typeparamref name="T"/>, or <c>null</c> if the request fails or the deserialization is unsuccessful.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails.</exception>
    Task<T?> GetApiResponse<T>(string apiUrl, IntegrationSettingKeys settingKeys);
}