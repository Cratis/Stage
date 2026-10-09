// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_scoped_shared_declarations;

public class with_a_protected_operation : given.a_multi_module_application
{
    ArtifactRenderPlan _plan = null!;
    AuthorizationPolicyRegistration[] _scopedRegistrations = [];
    AuthorizationPolicyRegistration[] _applicationRegistrations = [];

    void Because()
    {
        _plan = Plan(new(ArtifactRenderScopeKind.Slice, _placeOrder.Id));
        var registration = Files(_application).Single(file => file.RelativePath == "GeneratedPolicyRegistration.cs");
        _scopedRegistrations = Registrations(RenderedOutput.Load(Files(_plan).Append(registration)));
        _applicationRegistrations = Registrations(RenderedOutput.Load(Files(_application)));
    }

    [Fact] void should_include_the_same_policy_runtime() => SameArtifact(_application, _plan, "GeneratedPolicies/Policies.cs").ShouldBeTrue();
    [Fact] void should_include_the_same_operation_policy() => SameArtifact(_application, _plan, $"GeneratedPolicies/{SemanticPolicyArtifactRenderer.Name(_placeOrder.Commands.Single().Id)}.cs").ShouldBeTrue();
    [Fact] void should_exclude_other_operations_policies() => _plan.Artifacts.Count(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/StagePolicy_", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_register_only_the_selected_policy() => _scopedRegistrations.Select(registration => registration.Name).ShouldContainOnly([SemanticPolicyArtifactRenderer.Name(_placeOrder.Commands.Single().Id)]);
    [Fact] void should_preserve_the_authenticated_only_registration() => _scopedRegistrations.Single().EvaluatesAnonymous.ShouldBeFalse();
    [Fact] void should_register_the_application_in_ordinal_name_order() => _applicationRegistrations.Select(registration => registration.Name).SequenceEqual(_applicationRegistrations.Select(registration => registration.Name).Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_foreign_operations_anonymous_opt_in() => _applicationRegistrations.Single(registration => registration.Name == SemanticPolicyArtifactRenderer.Name(_customers.Features.Single().Slices.Single().Commands.Single().Id)).EvaluatesAnonymous.ShouldBeTrue();

    static AuthorizationPolicyRegistration[] Registrations(System.Reflection.Assembly assembly)
    {
        var services = new ServiceCollection();
        assembly.GetType("Shop.GeneratedPolicies.Registration")!.GetMethod("Register")!.Invoke(null, [services]);
        return [.. services.Select(service => service.ImplementationInstance).OfType<AuthorizationPolicyRegistration>()];
    }
}
