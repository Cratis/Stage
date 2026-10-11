// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class optional_scalar_response_cases
{
    internal static ExecutableSemanticModel Compile(string source)
    {
        var model = invoice_model.Compile(source + Environment.NewLine + Source);

        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(WithNullFixtures)] })]
        });
    }

    static SemanticFeature WithNullFixtures(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(WithNullFixtures)],
        Slices = [.. feature.Slices.Select(slice => slice with { Specifications = [.. slice.Specifications.Select(WithNullFixture)] })]
    };

    // Screenplay 4.133.3 rejects null literals in authored command fixtures (PLAY0350), while its ESM evaluator
    // accepts optional null inputs. Exercise that typed ESM contract directly without changing the compiler.
    static SemanticSpecification WithNullFixture(SemanticSpecification specification) => specification.Name.EndsWith("Null", StringComparison.Ordinal)
        ? specification with { When = specification.When! with { Values = [.. specification.When.Values.Select(value => value with { Value = SemanticValue.Null })] } }
        : specification;

    const string Source = """
        concept Label : String
        module Optional
          feature Responses
            slice StateChange Number
              command ReturnNumber
                value Int optional
                returns value
              specification OptionalNumberPresent
                when ReturnNumber
                  value = 42
                then returns 42
              specification OptionalNumberNull
                when ReturnNumber
                  value = 42
                then returns null
              specification WrongOptionalNumberPresent
                when ReturnNumber
                  value = 42
                then returns 99
              specification WrongOptionalNumberNull
                when ReturnNumber
                  value = 42
                then returns 99
            slice StateChange Text
              command ReturnText
                value String optional
                returns value
              specification OptionalTextNull
                when ReturnText
                  value = "Apollo"
                then returns null
              specification WrongOptionalTextNull
                when ReturnText
                  value = "Apollo"
                then returns "Different"
            slice StateChange Concept
              command ReturnLabel
                value Label optional
                returns value
              specification OptionalConceptNull
                when ReturnLabel
                  value = "Apollo"
                then returns null
              specification WrongOptionalConceptNull
                when ReturnLabel
                  value = "Apollo"
                then returns "Different"
        """;
}
