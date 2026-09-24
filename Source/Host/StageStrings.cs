// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Strings;

namespace Cratis.Stage.Host;

/// <summary>
/// Discovers and merges the <c language="csharp">.strings</c> files sitting beside the model's
/// <c language="csharp">.play</c> files into the flat locale dictionaries a frontend resolves
/// <c language="csharp">$strings.&lt;key&gt;</c> references against.
/// </summary>
/// <remarks>
/// Resolution happens in the frontend, at runtime, on purpose - a locale switch or an edited translation
/// takes effect without recompiling the model. This is the other half of that: the filesystem access
/// only Stage's host has, exposed as one dictionary per locale rather than making a frontend discover and
/// parse files itself over an API never designed to serve raw file contents.
/// </remarks>
/// <param name="root">The root directory the model's <c language="csharp">.play</c> and <c language="csharp">.strings</c> files live under.</param>
public class StageStrings(string root)
{
    readonly StringsFiles _files = new();

    /// <summary>
    /// Gets every locale at least one discovered <c language="csharp">.strings</c> file declares.
    /// </summary>
    /// <returns>The distinct locales, in no particular order.</returns>
    public IEnumerable<string> Locales() =>
        _files.FindIn(root)
            .Select(file => file.Locale)
            .Where(locale => locale is not null)
            .Distinct(StringComparer.Ordinal)!;

    /// <summary>
    /// Builds the merged dictionary for one locale, from every <c language="csharp">.strings</c> file that declares it.
    /// </summary>
    /// <param name="locale">The locale to build the dictionary for.</param>
    /// <returns>Every entry from every matching file, keyed by its dotted path.</returns>
    /// <remarks>
    /// A key declared in more than one file - the same concept used from a module-level and a slice-level
    /// <c language="csharp">.strings</c> file, say - takes its value from whichever file <see cref="IStringsFiles.FindIn"/>
    /// returns last; discovery orders by relative path, so this is deterministic even though it is not
    /// itself a conflict-detection mechanism. Authors are expected to give a key the same value everywhere
    /// it is declared, the same way two modules never redeclare the same read model.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Dictionary(string locale)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in _files.FindIn(root).Where(file => file.Locale == locale))
        {
            var parsed = StringsFile.Parse(_files.ReadContent(file));
            foreach (var entry in parsed.Entries)
            {
                entries[entry.Key] = entry.Value;
            }
        }

        return entries;
    }
}
