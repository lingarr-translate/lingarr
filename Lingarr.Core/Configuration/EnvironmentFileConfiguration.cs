using System.Collections;

namespace Lingarr.Core.Configuration;

public static class EnvironmentFileConfiguration
{
    private const string FileSuffix = "_FILE";

    public static void Apply()
    {
        foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
        {
            var fileVariableName = (string)variable.Key;
            if (!fileVariableName.EndsWith(FileSuffix, StringComparison.Ordinal) ||
                fileVariableName.Length == FileSuffix.Length)
            {
                continue;
            }

            var path = variable.Value as string;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                continue;
            }

            var variableName = fileVariableName[..^FileSuffix.Length];
            try
            {
                Environment.SetEnvironmentVariable(variableName, File.ReadAllText(path).TrimEnd('\r', '\n'));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Warning: Could not read '{fileVariableName}' from '{path}': {exception.Message}");
            }
        }
    }
}
