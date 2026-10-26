// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer;

public class when_rendering_an_identity_invocation : given.a_reaction_specification
{
    void Establish() => PrepareIdentity("DeclaredIdentityAllows");
    void Because() => Render();
    [Fact] void should_drive_the_generated_reactor() => _source.ShouldContain("ReactorScenario<TrustedWorker>");
    [Fact] void should_use_a_deterministic_placeholder_for_the_sourceless_append() => _source.ShouldContain("ForEventSource(\"when-append\")");
    [Fact] void should_read_identity_from_the_reactor_attribute() => _source.ShouldContain("GetCustomAttribute<ExecuteCommandsAsSystemAttribute>()");
    [Fact] void should_establish_only_the_roles_on_that_attribute() => _source.ShouldContain("AsSystem([.. identity.Roles])");
    [Fact] void should_execute_the_recorded_command() => _source.ShouldContain("_result = await _scenario.Execute(command);");
    [Fact] void should_register_the_generated_policy() => _source.ShouldContain("Registration.Register(_scenario.Services);");
    [Fact] void should_dispose_both_scenarios() => _source.ShouldContain("_reactor.Dispose();");
}
