// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_generating_from_the_restored_scaffold : given.a_restored_scaffold
{
    string _checkedIn = null!;

    void Because()
    {
        if (Environment.GetEnvironmentVariable("STAGE_WRITE_FRAMEWORK_NAME_CATALOG") == "1")
        {
            File.WriteAllText(Path.Combine(RepositoryRoot().FullName, "Source", "Rendering.Cratis", "Naming", "Imports", "FrameworkNames.catalog"), _generatedText);
            _checkedIn = _generatedText;
            return;
        }

        using var stream = typeof(FrameworkNameCatalog).Assembly.GetManifestResourceStream(FrameworkNameCatalog.ResourceName)!;
        using var reader = new StreamReader(stream);
        _checkedIn = reader.ReadToEnd();
    }

    [Fact] void should_record_the_restored_package_versions_and_pins() => Assert.True(Same("pin", "package", "framework", "scaffold"), Difference("pin", "package", "framework", "scaffold"));
    [Fact] void should_record_the_global_usings_of_each_project() => Assert.True(Same("using"), Difference("using"));
    [Fact] void should_record_the_names_in_the_compile_assets() => Assert.True(Same("scope", "namespace", "type", "extensions", "base"), Difference("scope", "namespace", "type", "extensions", "base"));
    [Fact] void should_match_the_checked_in_catalog_byte_for_byte() => Assert.True(_generatedText == _checkedIn, Difference());

    bool Same(params string[] records) => Lines(_generatedText, records).SequenceEqual(Lines(_checkedIn, records), StringComparer.Ordinal);

    string Difference(params string[] records)
    {
        var generated = Lines(_generatedText, records).ToArray();
        var checkedIn = Lines(_checkedIn, records).ToArray();
        var added = generated.Except(checkedIn, StringComparer.Ordinal).Take(20).Select(line => $"+ {line}");
        var removed = checkedIn.Except(generated, StringComparer.Ordinal).Take(20).Select(line => $"- {line}");
        return $"The checked-in framework name catalog is stale. Regenerate it with: {FrameworkNameCatalogText.RegenerateCommand}{Environment.NewLine}" +
            string.Join(Environment.NewLine, removed.Concat(added));
    }

    static IEnumerable<string> Lines(string text, string[] records) =>
        text.Split('\n').Where(line => records.Length == 0 || records.Contains(line.Split('\t')[0], StringComparer.Ordinal));
}
