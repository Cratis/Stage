// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class named_value_command_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        var slices = new List<string>();
        foreach (var kind in new[] { "Equal", "NotEqual" })
        {
            foreach (var optional in new[] { false, true })
            {
                foreach (var message in new[] { "Default", "Error", "Warning", "Information" })
                {
                    var name = $"{kind}{(optional ? "Optional" : "Required")}{message}";
                    var severity = message == "Default" ? string.Empty : $" severity {message.ToLowerInvariant()} message \"Authored {name}\"";
                    var expected = message == "Default" ? $"A value must {(kind == "Equal" ? "equal" : "not equal")} 'inReview'." : $"Authored {name}";
                    var valid = kind == "Equal" ? "inReview" : "draft";
                    var invalid = kind == "Equal" ? "draft" : "inReview";
                    var rule = $"value {(kind == "Equal" ? "==" : "!=")} \"inReview\"{severity}";
                    slices.Add(Slice(name, optional ? "NumberedStatus optional" : "Status", $"validate\n          {rule}", valid, invalid, expected, optional));
                }
            }
        }

        slices.Add(Slice("RequirementEqual", "Status", "validate\n          require value == inReview", "inReview", "draft", "Command requirement was not met."));
        slices.Add(Slice("RequirementNotEqual", "NumberedStatus", "validate\n          require value != \"draft\"\n            severity warning\n            message \"Not draft\"", "inReview", "draft", "Not draft"));
        slices.Add(Slice("RequirementSameConcept", "Status", "validate\n          require value == other\n            severity information\n            message \"Same status\"", "inReview", "draft", "Same status"));
        slices.Add(Slice("RequirementDifferentMember", "Status", "validate\n          require value != other\n            message \"Different status\"", "draft", "inReview", "Different status"));
        slices.Add(Slice("RequirementAnd", "Status", "validate\n          require value == inReview and value != draft", "inReview", "draft", "Command requirement was not met."));
        slices.Add(Slice("RequirementOr", "Status", "validate\n          require value == inReview or value != draft", "inReview", "draft", "Command requirement was not met."));
        var source = "concept Status : Enum\n  draft\n  inReview\nconcept NumberedStatus : Enum\n  draft = -7\n  inReview = 42\nmodule Inputs\n  feature NamedValues\n" + string.Join('\n', slices);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NamedValues"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("named-values"), "named-values", "NamedValues.play", source);
        var compilation = new SemanticModelCompiler().Compile("NamedValues", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;

        // Authored fixtures refuse explicit null. Pin absence through the optional typed ESM contract.
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

    static string Slice(string name, string type, string rules, string valid, string invalid, string message, bool optional = false)
    {
        var specifications = new[] { "Valid", "Invalid" }.Concat(optional ? ["Absent"] : Array.Empty<string>()).Select(outcome => $$"""
                  specification {{name}}{{outcome}}
                    when Check{{name}}
                      id = "one"
                      value = "{{(outcome == "Invalid" ? invalid : valid)}}"
                      other = "inReview"
                    {{(outcome == "Invalid" ? $"then error \"{message}\"" : "then returns \"one\"")}}
            """);
        return $$"""
                slice StateChange {{name}}Input
                  command Check{{name}}
                    id String
                    value {{type}}
                    other {{type.Replace(" optional", string.Empty, StringComparison.Ordinal)}}
                    {{rules}}
                    returns id
            {{string.Join('\n', specifications)}}
            """;
    }
}
#endif
