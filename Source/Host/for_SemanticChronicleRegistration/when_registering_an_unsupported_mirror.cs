// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_an_unsupported_mirror : a_chronicle_registration
{
    void Establish() => UseSource(Source.Replace("name ProjectName\n      query ProjectById", "name ProjectName?\n      query ProjectById", StringComparison.Ordinal));

    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, "Projects", _plan, _mirrorIssues);

    [Fact] void should_not_register_a_partial_mirror() => _registrations.ToArray().ShouldEqual(["events"]);
    [Fact] void should_report_the_unsupported_mirror_capability() => _mirrorIssues.Single().Capability.ShouldEqual("ProjectionMirror");
    [Fact] void should_identify_the_projection_by_its_semantic_id() => _mirrorIssues.Single().Artifact.ShouldEqual(_plan.Projections.Values.Single().Id.ToString());
    [Fact] void should_explain_why_the_mirror_is_not_visible() => _mirrorIssues.Single().Details.ShouldContain("not visible in the Workbench");
    [Fact] void should_rebuild_without_the_mirror() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
}
