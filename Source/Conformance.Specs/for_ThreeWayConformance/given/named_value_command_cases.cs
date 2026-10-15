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
        var slices = ValidationCases().Concat(RequirementCases()).Select(Slice);
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

    static IEnumerable<CommandCase> ValidationCases() =>
        from kind in new[] { "Equal", "NotEqual" }
        from optional in new[] { false, true }
        from message in new[] { "Default", "Error", "Warning", "Information" }
        select ValidationCase(kind, optional, message);

    static CommandCase ValidationCase(string kind, bool optional, string message)
    {
        var name = $"{kind}{(optional ? "Optional" : "Required")}{message}";
        var severity = message == "Default" ? string.Empty : $" severity {message.ToLowerInvariant()} message \"Authored {name}\"";
        var expected = message == "Default" ? $"A value must {(kind == "Equal" ? "equal" : "not equal")} 'inReview'." : $"Authored {name}";
        var valid = kind == "Equal" ? "inReview" : "draft";
        var invalid = kind == "Equal" ? "draft" : "inReview";
        var rule = $"value {(kind == "Equal" ? "==" : "!=")} \"inReview\"{severity}";
        return new(name, optional ? "NumberedStatus optional" : "Status", $"validate\n          {rule}")
        {
            Valid = valid,
            Invalid = invalid,
            Message = expected,
            Optional = optional
        };
    }

    static IEnumerable<CommandCase> RequirementCases() =>
    [
        new("RequirementEqual", "Status", "validate\n          require value == inReview"),
        new("RequirementNotEqual", "NumberedStatus", "validate\n          require value != \"draft\"\n            severity warning\n            message \"Not draft\"") { Message = "Not draft" },
        new("RequirementSameConcept", "Status", "validate\n          require value == other\n            severity information\n            message \"Same status\"") { Message = "Same status" },
        new("RequirementDifferentMember", "Status", "validate\n          require value != other\n            message \"Different status\"") { Valid = "draft", Invalid = "inReview", Message = "Different status" },
        new("RequirementAnd", "Status", "validate\n          require value == inReview and value != draft"),
        new("RequirementOr", "Status", "validate\n          require value == inReview or value != draft")
    ];

    static string Slice(CommandCase vector)
    {
        var specifications = new[] { "Valid", "Invalid" }.Concat(vector.Optional ? ["Absent"] : Array.Empty<string>()).Select(outcome => $$"""
                  specification {{vector.Name}}{{outcome}}
                    when Check{{vector.Name}}
                      id = "one"
                      value = "{{(outcome == "Invalid" ? vector.Invalid : vector.Valid)}}"
                      other = "inReview"
                    {{(outcome == "Invalid" ? $"then error \"{vector.Message}\"" : "then returns \"one\"")}}
            """);
        return $$"""
                slice StateChange {{vector.Name}}Input
                  command Check{{vector.Name}}
                    id String
                    value {{vector.Type}}
                    other {{vector.Type.Replace(" optional", string.Empty, StringComparison.Ordinal)}}
                    {{vector.Rules}}
                    returns id
            {{string.Join('\n', specifications)}}
            """;
    }

    sealed record CommandCase(string Name, string Type, string Rules)
    {
        internal string Valid { get; init; } = "inReview";
        internal string Invalid { get; init; } = "draft";
        internal string Message { get; init; } = "Command requirement was not met.";
        internal bool Optional { get; init; }
    }
}
#endif
