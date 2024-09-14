using Microsoft.AspNetCore.Mvc;

namespace Lingarr.Server.Interfaces.Services;

/// <summary>
/// Defines a service for interacting with the Radarr API.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Asynchronously retrieves a response from the specified API URL and returns it as a file stream.
    /// </summary>
    /// <param name="url">The URL of the API to fetch the response from.</param>
    /// <returns>
    /// A <see cref="FileStreamResult"/> if the API response is successful, containing the response content stream and appropriate content type.
    /// If the response is not successful or an error occurs, a <see cref="NotFoundResult"/> is returned.
    /// </returns>
    /// <exception cref="Exception">Thrown if an error occurs while making the HTTP request or processing the response.</exception>
    Task<IActionResult> GetApiResponse(string url);
}