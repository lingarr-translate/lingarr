using Lingarr.Core.Entities;
using Lingarr.Server.Models.Api;

namespace Lingarr.Server.Interfaces.Services;

public interface IMappingService
{
    /// <summary>
    /// Retrieves all current path mappings.
    /// </summary>
    /// <returns>A list of path mapping DTOs containing source path, destination path, and media type information.</returns>
    Task<List<PathMappingDto>> GetMapping();

    /// <summary>
    /// Updates all path mappings by replacing existing mappings with the provided ones.
    /// </summary>
    /// <param name="mappings">The new list of path mappings to set.</param>
    /// <remarks>This operation removes all existing mappings before adding the new ones.</remarks>
    Task SetMapping(List<PathMapping> mappings);
}