namespace Lingarr.Server.Models.Api;

public class PathMappingDto
{
    public required string SourcePath { get; set; }
    public required string DestinationPath { get; set; }
    public required string MediaType { get; set; }
}