using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class ThresholdRequest
{
    public MediaType MediaType { get; set; }
    public int Id { get; set; }
    public int Hours { get; set; }
}