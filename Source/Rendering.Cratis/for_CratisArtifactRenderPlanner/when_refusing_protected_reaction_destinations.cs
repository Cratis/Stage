// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_protected_reaction_destinations
{
    const string Source = """
        concept ReactionDestination : String
        module Work
          feature Processing
            slice StateChange Start
              command StartWork
                id String identifier
                token ReactionDestination
                produces event Started
                  for id
                  token ReactionDestination = token
            slice Automation FollowUp
              event Completed
                amount Decimal
              reaction Complete
                when Started
                  produces Completed
                    for token
                    amount = 5
        """;

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void should_refuse_the_protected_destination_without_artifacts(bool personal, bool scoped)
    {
        var model = invoice_model.Compile(Source);
        var application = model.Application with
        {
            Concepts = [.. model.Application.Concepts.Select(concept => concept with { Compliance = personal ? new(new(), null) : new(null, new()) })]
        };

        // Exercise target admission even when a producer bypasses upstream compliance validation.
        Set(model, nameof(model.Application), application);
        Set(model, nameof(model.LanguageVersion), LanguageVersion.V13);
        Set(model, nameof(model.SemanticVersion), SemanticVersion.V13);
        var start = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Start");
        var scope = scoped ? new ArtifactRenderScope(ArtifactRenderScopeKind.Slice, start.Id) : null;
        var plan = invoice_model.Plan(model, scope);
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-040"]);
        plan.Diagnostics.Single().Message.ShouldContain("reaction destination 'Complete'");
        plan.Artifacts.ShouldBeEmpty();
    }

    static void Set(ExecutableSemanticModel model, string property, object value) =>
        typeof(ExecutableSemanticModel).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, value);
}
