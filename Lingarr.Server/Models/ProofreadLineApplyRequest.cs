using Lingarr.Core.Enum;

namespace Lingarr.Server.Models;

public class ProofreadLineApplyRequest
{
    public int Id { get; set; }
    public int Position { get; set; }
    public required string Target { get; set; }
    public ProofreadLineOrigin? Origin { get; set; }
}
