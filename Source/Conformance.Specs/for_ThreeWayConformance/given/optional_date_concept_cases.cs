// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class optional_date_concept_cases
{
    internal static ExecutableSemanticModel Compile()
    {
        const string source = """
            concept Moment : DateTime pii
            module Dates
              feature Windows
                slice StateChange Check
                  command Check
                    id String
                    at Moment optional
                    validate
                      at >= "2026-02-01T00:00:00Z" message "Command minimum."
                    returns id
                  specification Present
                    when Check
                      id = "one"
                      at = "2026-10-17T02:00:00.1234567+02:00"
                    then returns "one"
                  specification Absent
                    when Check
                      id = "one"
                      at = "2026-10-17T00:00:00Z"
                    then returns "one"
                  specification Boundary
                    when Check
                      id = "one"
                      at = "2026-02-01T02:00:00+02:00"
                    then returns "one"
                  specification Invalid
                    when Check
                      id = "one"
                      at = "2026-02-01T00:00:00.0000001+02:00"
                    then error "Command minimum."
                  specification TickBefore
                    when Check
                      id = "one"
                      at = "2026-01-31T23:59:59.9999999Z"
                    then error "Command minimum."
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NominalDates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("nominal-dates"), "nominal-dates", "NominalDates.play", source);
        var compilation = new SemanticModelCompiler().Compile("NominalDates", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;

        // Authored fixtures reject explicit null literals; exercise the nullable typed ESM contract.
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification.Name == "Absent"
                            ? specification with { When = specification.When with { Values = [.. specification.When.Values.Select(value => slice.Commands.Single().Properties.Single(property => property.Id == value.TargetProperty).Name == "at" ? value with { Value = SemanticValue.Null } : value)] } }
                            : specification)]
                    })]
                })]
            })]
        });
    }
}
#endif
