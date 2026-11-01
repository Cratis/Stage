// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class requirement_concept_order_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        const string declarations = """
            concept Label : String
              validate
                min 2 severity error message "Label short"
                != "bad" severity warning message "Label bad"
            concept Count : Int
              validate
                > 5 severity error message "Count small"
            module Inputs
              feature Ordered
                slice StateChange Checking
                  command Check
                    id String
                    zeta Label
                    alpha Count
                    extra Count optional
                    validate
                      zeta != "x" severity error message "Command rule"
                      require id != "draft"
                        severity error
                        message "Requirement"
                    returns id
            """;
        var cases = new[]
        {
            new OrderCase("AllValid", "ok", 7, 7, "one"),
            new OrderCase("RequirementOnly", "ok", 7, 7, "draft", "Requirement"),
            new OrderCase("ConceptOnly", "", 7, 7, "one", "Label short"),
            new OrderCase("RuleConceptRequirement", "x", 7, 7, "draft", "Command rule"),
            new OrderCase("TwoConceptsRequirement", "", 2, 7, "draft", "Label short"),
            new OrderCase("MixedSeverities", "bad", 2, 7, "draft", "Label bad"),
            new OrderCase("OptionalAbsent", "ok", 7, 7, "draft", "Requirement"),
            new OrderCase("OptionalInvalid", "ok", 7, 2, "draft", "Count small"),
            new OrderCase("OptionalValid", "ok", 7, 8, "one")
        };
        var specifications = cases.Select(vector => $$"""
                  specification {{vector.Name}}
                    when Check
                      id = "{{vector.Id}}"
                      zeta = "{{vector.Zeta}}"
                      alpha = {{vector.Alpha}}
                      extra = {{vector.Extra}}
                    {{(vector.Message is null ? "then returns \"one\"" : $"then error \"{vector.Message}\"")}}
            """);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("RequirementConceptOrder"));
        var source = declarations + "\n" + string.Join('\n', specifications);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("requirement-concept-order"), "requirement-concept-order", "RequirementConceptOrder.play", source);
        var compilation = new SemanticModelCompiler().Compile("RequirementConceptOrder", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;

        // Authored fixtures reject explicit null literals; pin the optional typed ESM contract.
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification.Name == "OptionalAbsent"
                            ? specification with { When = specification.When! with { Values = [.. specification.When.Values.Select(value => slice.Commands.Single().Properties.Single(property => property.Id == value.TargetProperty).Name == "extra" ? value with { Value = SemanticValue.Null } : value)] } }
                            : specification)]
                    })]
                })]
            })]
        });
    }

    sealed record OrderCase(string Name, string Zeta, int Alpha, int Extra, string Id, string? Message = null);
}
#endif
