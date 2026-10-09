// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyArtifactRenderer;

public class when_rendering_an_unkeyed_list_policy : for_SemanticCratisAdmission.given.a_query_shape
{
    IAuthorizationPolicy _policy = null!;
    AuthorizationPolicyRegistration[] _registrations = [];
    string _stateView = string.Empty;

    void Establish()
    {
        Configure(SemanticQueryCardinality.Many, SemanticQueryDelivery.Live, false);
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { Policies = [new("Staff", new SemanticRoleCondition("Staff"))] });
        _query = _query with { Authorization = new SemanticPolicyReference("Staff") };
        _slice = _slice with { Queries = [_query] };
    }

    void Because()
    {
        Evaluate();
        var policy = SemanticPolicyArtifactRenderer.Render(_context, [_context.Slice(_slice.Id)]);
        var registration = new RenderedFile("Registration.cs", $$"""
            namespace {{_context.RootNamespace}}.GeneratedPolicies;
            public static partial class Registration
            {
                public static void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services) => RegisterGenerated(services);
                static partial void RegisterGenerated(Microsoft.Extensions.DependencyInjection.IServiceCollection services);
            }
            """);
        var assembly = RenderedOutput.Load([.. policy, registration]);
        var services = new ServiceCollection();
        assembly.GetType($"{_context.RootNamespace}.GeneratedPolicies.Registration")!.GetMethod("Register")!.Invoke(null, [services]);
        _registrations = [.. services.Where(service => service.ServiceType == typeof(AuthorizationPolicyRegistration)).Select(service => (AuthorizationPolicyRegistration)service.ImplementationInstance!)];
        _policy = (IAuthorizationPolicy)Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == SemanticPolicyArtifactRenderer.Name(_query.Id)))!;
        _stateView = SemanticStateViewArtifactRenderer.Render(_context.Slice(_slice.Id), _context).Content;
    }

    [Fact] void should_admit_the_argument_free_policy() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_the_policy() => _registrations.Length.ShouldEqual(1);
    [Fact] void should_emit_the_matching_authorization_attribute() => _stateView.ShouldContain(SemanticPolicyArtifactRenderer.Name(_query.Id));
    [Fact] void should_allow_the_declared_role() => Allows(new(new ClaimsIdentity([new(ClaimTypes.Role, "Staff")], "fixture"))).ShouldBeTrue();
    [Fact] void should_deny_a_guest() => Allows(new(new ClaimsIdentity())).ShouldBeFalse();

    bool Allows(ClaimsPrincipal principal) => _policy.IsAuthorized(new(principal, GetType(), new object()), CancellationToken.None).AsTask().GetAwaiter().GetResult();
}
