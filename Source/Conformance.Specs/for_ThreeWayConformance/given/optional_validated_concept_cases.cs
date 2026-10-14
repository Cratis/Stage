// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class optional_validated_concept_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        var concepts = """
            concept Count : Int
              validate
                > 5 severity error message "Count minimum"
                < 10 severity warning message "Count maximum"
                != 2 severity information message "Count excluded"
            concept Amount : Decimal
              validate
                > 5 severity error message "Amount minimum"
                < 10 severity warning message "Amount maximum"
                != 2 severity information message "Amount excluded"
            concept Label : String
              validate
                length == 3 severity error message "Label length"
                matches "^X" severity warning message "Label pattern"
                != "bad" severity information message "Label excluded"
            module Inputs
              feature Optional
            """;
        var slices = new[] { ("Count", "7", "2", "Count minimum"), ("Amount", "7.5", "2", "Amount minimum"), ("Label", "\"Xxx\"", "\"bad\"", "Label pattern") }
            .Select(vector => $$"""
                    slice StateChange {{vector.Item1}}Input
                      command Check{{vector.Item1}}
                        id String
                        value {{vector.Item1}} optional
                        returns id
                      specification {{vector.Item1}}Absent
                        when Check{{vector.Item1}}
                          id = "one"
                          value = {{vector.Item2}}
                        then returns "one"
                      specification {{vector.Item1}}Valid
                        when Check{{vector.Item1}}
                          id = "one"
                          value = {{vector.Item2}}
                        then returns "one"
                      specification {{vector.Item1}}Invalid
                        when Check{{vector.Item1}}
                          id = "one"
                          value = {{vector.Item3}}
                        then error "{{vector.Item4}}"
                """);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OptionalInputs"));
        var source = concepts + "\n" + string.Join('\n', slices);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("optional-inputs"), "optional-inputs", "OptionalInputs.play", source);
        var compilation = new SemanticModelCompiler().Compile("OptionalInputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;

        // Authored command fixtures reject explicit null literals. Exercise the optional typed ESM contract.
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification.Name.EndsWith("Absent", StringComparison.Ordinal)
                            ? specification with { When = specification.When! with { Values = [.. specification.When.Values.Select(value => slice.Commands.Single().Properties.Single(property => property.Id == value.TargetProperty).Name == "value" ? value with { Value = SemanticValue.Null } : value)] } }
                            : specification)]
                    })]
                })]
            })]
        });
    }
}
#endif
