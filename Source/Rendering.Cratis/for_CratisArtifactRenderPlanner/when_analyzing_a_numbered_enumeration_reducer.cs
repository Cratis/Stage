// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_analyzing_a_numbered_enumeration_reducer : Specification
{
    PureTransitionAdmission.Verdict _verdict = null!;

    async Task Because()
    {
        const string source = "concept Status : Enum\n  draft = -1\n  issued = 90\n" + when_rendering_a_pure_reducer.Source;
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        var request = new ArtifactRenderRequest(
            loaded.Model,
            loaded.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = context.Reducers.Single();
        _verdict = PureTransitionAdmission.Analyze("""
            switch (Status.Draft)
            {
                case Status.Draft: return new Total(context.Event.Id, context.Event.Amount);
                case 0: return null;
                default: return null;
            }
            """,
            context,
            context.ReadModels[reducer.ReadModel],
            context.Events[reducer.Transitions.Single().EventContract],
            loaded.TypedContextDescriptors.Single(),
            loaded.ImplementationRequirements.Single());
    }

    // With positional stubs, Draft and zero collide and cause a compilation refusal (019).
    // Correct numbered stubs compile, then the existing purity gate refuses the numeric enum conversion (022).
    [Fact] void should_bind_the_negative_member_without_a_duplicate_zero_case() => _verdict.Code.ShouldEqual("STAGE-ESM-022");
    [Fact]
    void should_keep_explicit_numbers_in_analysis_declarations() => SemanticEnumMembers.Declarations(
        given.invoice_model.Compile("concept Status : Enum\n  draft = -1\n  issued = 90\nmodule Orders\n  feature Ordering\n    slice StateChange Place\n      event Placed\n        status Status").Application.Concepts.Single()).ShouldContainOnly("Draft = -1", "Issued = 90");
}
#endif
