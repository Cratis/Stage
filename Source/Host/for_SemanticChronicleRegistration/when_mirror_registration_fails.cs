// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_mirror_registration_fails : a_chronicle_registration
{
    void Establish() => _services.ReadModels.RegisterMany(Arg.Any<RegisterManyRequest>()).ThrowsAsync(new Exception("Chronicle registration unavailable"));

    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, "Projects", _plan, _mirrorIssues);

    [Fact] void should_report_the_mirror_registration_failure() => _mirrorIssues.Single().Details.ShouldContain("Workbench mirror registration failed: Chronicle registration unavailable");
    [Fact] void should_report_the_mirror_capability() => _mirrorIssues.Single().Capability.ShouldEqual("ProjectionMirror");
    [Fact] void should_not_retry_a_write_with_an_unknown_outcome() => _ = _services.ReadModels.Received(1).RegisterMany(Arg.Any<RegisterManyRequest>());
    [Fact] void should_not_continue_dependent_registration() => _ = _services.Projections.DidNotReceive().Register(Arg.Any<RegisterRequest>());
    [Fact] void should_preserve_the_rebuilt_world() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
}
