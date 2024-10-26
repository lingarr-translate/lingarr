
using Lingarr.Core.Enum;

namespace Lingarr.Core.Entities;

public class PathMapping : BaseEntity
{
    public required string SourcePath { get; set; }
    public required string DestinationPath { get; set; }
    public required MediaType MediaType { get; set; }
}