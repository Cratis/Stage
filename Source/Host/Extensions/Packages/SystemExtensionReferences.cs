// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Reads which extension each system of a model names, from the model's <c lang="text">.play</c> files.
/// </summary>
public static class SystemExtensionReferences
{
    /// <summary>
    /// Reads the references.
    /// </summary>
    /// <param name="modelPath">The model's folder or one of its files.</param>
    /// <returns>The extension reference by system name.</returns>
    public static IReadOnlyDictionary<string, string> Read(string modelPath)
    {
        var references = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in PlayFiles(modelPath)) Read(File.ReadLines(file), references);
        return references;
    }

    /// <summary>
    /// Reads the references from the lines of one file.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="references">Receives the extension reference by system name.</param>
    public static void Read(IEnumerable<string> lines, IDictionary<string, string> references)
    {
        string? system = null;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#')) continue;

            if (!char.IsWhiteSpace(line[0]))
            {
                system = SystemName(line);
            }
            else if (system is not null && ExtensionReference(line) is { } reference)
            {
                references[system] = reference;
            }
        }
    }

    static string? SystemName(string line)
    {
        const string keyword = "system ";
        if (!line.StartsWith(keyword, StringComparison.Ordinal)) return null;

        var name = line[keyword.Length..].Trim();
        return name.Length > 0 && char.IsLetter(name[0]) && name.All(character => char.IsLetterOrDigit(character) || character == '_') ? name : null;
    }

    static string? ExtensionReference(string line)
    {
        const string keyword = "extension \"";
        var text = line.TrimStart();
        if (!text.StartsWith(keyword, StringComparison.Ordinal)) return null;

        var end = text.IndexOf('"', keyword.Length);
        return end > keyword.Length ? text[keyword.Length..end] : null;
    }

    static IEnumerable<string> PlayFiles(string modelPath)
    {
        if (!Directory.Exists(modelPath)) return File.Exists(modelPath) ? [modelPath] : [];
        return Directory.EnumerateFiles(modelPath, "*.play", SearchOption.AllDirectories).Where(file => !file.Contains($"{Path.DirectorySeparatorChar}.", StringComparison.Ordinal));
    }
}
