using Lingarr.Server.Models.Extraction;

namespace Lingarr.Server.Interfaces.Services;

public interface IProcessRunner
{
    /// <summary>
    /// Runs a program and returns its exit code and output.
    /// </summary>
    Task<ProcessResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken);
}
