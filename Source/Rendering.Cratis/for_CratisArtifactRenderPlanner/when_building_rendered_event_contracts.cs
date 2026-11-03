// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Builds a planned application whose public event and shared concept live in a referenced contracts project, once per configuration.
/// Opt in with STAGE_EVENT_CONTRACTS_BUILD=1 and STAGE_EVENT_CONTRACTS_PROOF_ROOT; each configuration runs as its own fact.
/// </summary>
public class when_building_rendered_event_contracts : Specification
{
#pragma warning disable CRSPEC0004 // EventContractsBuildFact derives from FactAttribute and is discovered by xUnit.
    [EventContractsBuildFact]
    async Task should_build_the_debug_application_and_its_referenced_contracts()
#pragma warning restore CRSPEC0004
        => await Prove("Debug");

#pragma warning disable CRSPEC0004 // EventContractsBuildFact derives from FactAttribute and is discovered by xUnit.
    [EventContractsBuildFact]
    async Task should_build_the_release_application_and_its_referenced_contracts()
#pragma warning restore CRSPEC0004
        => await Prove("Release");

#pragma warning disable CRSPEC0004 // EventContractsBuildFact derives from FactAttribute and is discovered by xUnit.
    [EventContractsBuildFact]
    async Task should_build_the_debug_event_target_reducer_against_the_contracts_project()
#pragma warning restore CRSPEC0004
        => await Prove("Debug", eventTargetReducer: true);

#pragma warning disable CRSPEC0004 // EventContractsBuildFact derives from FactAttribute and is discovered by xUnit.
    [EventContractsBuildFact]
    async Task should_build_the_release_event_target_reducer_against_the_contracts_project()
#pragma warning restore CRSPEC0004
        => await Prove("Release", eventTargetReducer: true);

    static async Task Prove(string configuration, bool eventTargetReducer = false)
    {
        var proof = new rendered_event_contracts(configuration, eventTargetReducer);
        proof.Render();
        await proof.Build();
        proof.AssertBuilt();

        // Only a passing proof removes its generated tree; a failing one keeps it for triage.
        proof.Complete();
    }
}
#endif
