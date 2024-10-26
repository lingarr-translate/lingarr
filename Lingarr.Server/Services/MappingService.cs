using Lingarr.Core.Data;
using Lingarr.Core.Entities;
using Lingarr.Server.Interfaces.Services;
using Lingarr.Server.Models.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;

namespace Lingarr.Server.Services;

public class MappingService : IMappingService
{
    private readonly LingarrDbContext _context;

    public MappingService(LingarrDbContext context)
    {
        _context = context;
    }
    
    /// <inheritdoc />
    public async Task<List<PathMappingDto>> GetMapping()
    {
        var mappings = await _context.PathMappings.ToListAsync();
        return mappings.Select(pathMapping => new PathMappingDto
        {
            SourcePath = pathMapping.SourcePath,
            DestinationPath = pathMapping.DestinationPath,
            MediaType = pathMapping.MediaType.GetDisplayName()
        }).ToList();
    }
    
    /// <inheritdoc />
    public async Task SetMapping(List<PathMapping> mappings)
    {
        _context.PathMappings.RemoveRange(_context.PathMappings);
        await _context.PathMappings.AddRangeAsync(mappings);

        await _context.SaveChangesAsync();
    }
}