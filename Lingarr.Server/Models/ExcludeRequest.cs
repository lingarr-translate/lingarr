using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class ExcludeRequest
{
    public MediaType MediaType { get; set; }
    public int Id { get; set; }
}