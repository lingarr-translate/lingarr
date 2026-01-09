using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class IncludeAllRequest
{
    public MediaType MediaType { get; set; }
    public bool Include { get; set; }
}
