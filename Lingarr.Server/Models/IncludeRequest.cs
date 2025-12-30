using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class IncludeRequest
{
    public MediaType MediaType { get; set; }
    public int Id { get; set; }
    public bool Include { get; set; }
}
