// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class optional_concept_command_rule_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        (string Name, string Type, string Rule, string Valid, string Invalid, string Message)[] vectors =
        [
            .. Numeric("Count", "5", "6", "7", "8", "9", "10", "11"),
            .. Numeric("Amount", "5.5", "6.5", "7.5", "8.5", "9.5", "10.5", "11.5"),
            ("FlagTrue", "Flag", "== true", "true", "false", "A value must equal true."),
            ("FlagFalse", "Flag", "== false", "false", "true", "A value must equal false."),
            ("FlagNotTrue", "Flag", "!= true", "false", "true", "A value must not equal true."),
            ("FlagNotFalse", "Flag", "!= false", "true", "false", "A value must not equal false."),
            ("CountOrdered", "Count", "> 5 severity warning message \"Count minimum\"\n          value != 2 severity information\n          value >= 6 message \"Count boundary\"", "7", "2", "Count minimum"),
            ("AmountOrdered", "Amount", "> 5.5 severity warning message \"Amount minimum\"\n          value != 2.5 severity information\n          value >= 6.5 message \"Amount boundary\"", "7.5", "2.5", "Amount minimum"),
            ("FlagOrdered", "Flag", "== false severity warning message \"Flag must be false\"\n          value != true severity information\n          value == false message \"Flag boundary\"", "false", "true", "Flag must be false")
        ];
        var slices = vectors.Select(vector => $$"""
                slice StateChange {{vector.Name}}Input
                  command Check{{vector.Name}}
                    id String
                    value {{vector.Type}} optional
                    validate
                      value {{vector.Rule}}
                    returns id
                  specification {{vector.Name}}Absent
                    when Check{{vector.Name}}
                      id = "one"
                      value = {{vector.Valid}}
                    then returns "one"
                  specification {{vector.Name}}Valid
                    when Check{{vector.Name}}
                      id = "one"
                      value = {{vector.Valid}}
                    then returns "one"
                  specification {{vector.Name}}Invalid
                    when Check{{vector.Name}}
                      id = "one"
                      value = {{vector.Invalid}}
                    then error "{{vector.Message}}"
            """);
        var source = "concept Count : Int\nconcept Amount : Decimal\nconcept Flag : Bool\nmodule Inputs\n  feature Optional\n" + string.Join('\n', slices);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("OptionalRules"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("optional-rules"), "optional-rules", "OptionalRules.play", source);
        var compilation = new SemanticModelCompiler().Compile("OptionalRules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;

        // Authored fixtures reject explicit null literals. Exercise the optional typed ESM contract.
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

    static IEnumerable<(string Name, string Type, string Rule, string Valid, string Invalid, string Message)> Numeric(string type, string minimum, string above, string equal, string different, string below, string maximum, string over) =>
    [
        ($"{type}Minimum", type, $"min {minimum}", minimum, "4", $"A value must be at least {minimum}."),
        ($"{type}Maximum", type, $"max {maximum}", maximum, over, $"A value must be at most {maximum}."),
        ($"{type}Equal", type, $"== {equal}", equal, different, $"A value must equal {equal}."),
        ($"{type}NotEqual", type, $"!= {equal}", different, equal, $"A value must not equal {equal}."),
        ($"{type}Greater", type, $"> {minimum}", above, minimum, $"A value must be greater than {minimum}."),
        ($"{type}AtLeast", type, $">= {minimum}", minimum, "4", $"A value must be at least {minimum}."),
        ($"{type}Less", type, $"< {maximum}", below, maximum, $"A value must be less than {maximum}."),
        ($"{type}AtMost", type, $"<= {maximum}", maximum, over, $"A value must be at most {maximum}.")
    ];
}
#endif
