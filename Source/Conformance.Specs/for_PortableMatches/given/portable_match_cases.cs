// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_PortableMatches.given;

internal static class portable_match_cases
{
    internal static readonly (string Pattern, string Input, bool Accepted)[] Cases =
    [
        ("^.$", "\r", false), ("^.$", "\u2028", false), ("^.$", "\u2029", false),
        (@"^\s$", "\u00a0", true), (@"^\s$", "\ufeff", true), (@"^\s$", "\u0085", false),
        (@"^\S$", "\u00a0", false), (@"^[\s]$", "\u2003", true), (@"^[^\s]$", "\u3000", false),
        (@"^\w$", "\u0130", false), (@"^\W$", "\u0130", true), (@"^[\w]$", "\u0130", false),
        (@"^\w$", "é", false), (@"^\d$", "\u0661", false),
        ("B", "ABC", true), ("^abc$", "ABC", false), ("^.$", "A", true),
        (@"^[^\D0]$", "1", true), (@"^[\w\W]$", "\n", true),
        ("^(a+)+b$|^a+$", new string('a', 64), true)
    ];

    internal static ExecutableSemanticModel Compile(string? refusal = null, bool concept = false)
    {
        using var resource = typeof(RegisterProjectCorpus).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalCorpus.Corpus.MatchEndOfInput.source.play")!;
        using var reader = new StreamReader(resource);
        var source = reader.ReadToEnd();
        var inputs = new Dictionary<string, string>();
        var cases = refusal is null ? Cases : [(refusal, "one", true)];
        for (var index = 0; index < cases.Length; index++)
        {
            var (pattern, input, accepted) = cases[index];
            foreach (var onConcept in refusal is null ? (bool[])[false, true] : [concept])
            {
                var name = $"Match{index}{(onConcept ? "Concept" : "Command")}";
                inputs.Add(name, input);
                var literal = "\"" + pattern.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
                if (onConcept) source += $"\nconcept {name} : String\n  validate\n    matches {literal} message \"Portable match rejected\"\n";
                source += $$"""

                    module {{name}}
                      feature Checking
                        slice StateChange Checking
                          command Check
                            id String
                            value {{(onConcept ? name : "String")}}
                    """;
                if (!onConcept) source += $"\n        validate\n          value matches {literal} message \"Portable match rejected\"";
                source += $$"""

                            returns id
                          specification Example
                            when Check
                              id = "one"
                              value = "input"
                            then {{(accepted ? "returns \"one\"" : "error \"Portable match rejected\"")}}
                    """;
            }
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("PortableMatches"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("matches"), "matches", "Matches.play", source);
        var compilation = new SemanticModelCompiler().Compile("PortableMatches", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        // Authored fixtures do not decode JSON Unicode escapes. Supply exact UTF-16 inputs in typed ESM,
        // so these cases test matching rather than the source literal spelling.
        var model = compilation.Value!.Model;
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => !inputs.TryGetValue(module.Name, out var input) ? module : module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with
                        {
                            When = specification.When! with
                            {
                                Values = [.. specification.When.Values.Select(value => slice.Commands.Single().Properties.Single(property => property.Id == value.TargetProperty).Name == "value"
                                    ? value with { Value = SemanticValue.Text(input) } : value)]
                            }
                        })]
                    })]
                })]
            })]
        });
    }
}
#endif
