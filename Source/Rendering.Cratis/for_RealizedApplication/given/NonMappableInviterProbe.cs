// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication.given;

/// <summary>
/// Exercises a declared persona that cannot be reduced to roles and claims and is the only persona allowed to invite.
/// </summary>
public static class NonMappableInviterProbe
{
    /// <summary>
    /// Gets the in-application specifications of the non-mappable inviter.
    /// </summary>
    public const string Source = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Security.Claims;
        using Cratis.Arc;
        using Cratis.Arc.AspNetCore.Http;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Http;
        using Cratis.Arc.Tenancy;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Lending.GeneratedAuthentication;
        using Lending.GeneratedOnboarding;
        using Microsoft.AspNetCore.Http;
        using Microsoft.Extensions.Caching.Memory;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Options;
        using NSubstitute;
        using Xunit;

        namespace Lending.OnboardingAuthorizationProbe;

        public class when_a_non_mappable_persona_invites
        {
            const string Tenant = "tenant-b";
            const string Persona = "Steward";

            [Fact]
            public void should_declare_the_persona_without_mapping_it()
            {
                var persona = Assert.Single(Personas.All, declared => declared.Name == Persona);
                Assert.False(persona.Mappable);
                Assert.True(Personas.Exists(Persona));
                Assert.Equal(new[] { Persona }, OnboardingRules.MemberInviters);
            }

            [Fact]
            public void should_give_the_persona_no_identity()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), Tenant, [Persona]);
                Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == MemberPersonaClaims.AuthenticationType);
                Assert.DoesNotContain(principal.Claims, claim => claim.Type == MemberPersonaClaims.PersonaClaimType);
                Assert.False(MemberPersonaClaims.Holds(principal, Tenant, Persona));
            }

            [Fact]
            public async Task should_deny_the_policy_to_a_caller_provisioned_with_the_persona()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), Tenant, [Persona]);
                var tenants = Substitute.For<ITenantIdAccessor>();
                tenants.Current.Returns(new TenantId(Tenant));
                Assert.False(await new InviteMemberPolicy(tenants).IsAuthorized(new AuthorizationPolicyContext(principal, typeof(InviteMemberToTenant), new InviteMemberToTenant("invitee@example.com", ["Member"])), CancellationToken.None));
            }

            [Fact]
            public async Task should_refuse_the_invitation_and_append_no_event()
            {
                using var scenario = new CommandScenario<InviteMemberToTenant>();
                var incoming = Incoming();
                var member = Assert.IsType<MemberIdentity>(MemberIdentity.From(incoming));
                scenario.Given.ForEventSource(member.Key).Events(new OnboardingMemberProvisioned(member.Provider, member.Subject, [Persona]));

                var context = new DefaultHttpContext { User = incoming };
                context.Request.Headers["x-cratis-tenant-id"] = Tenant;
                var requests = Substitute.For<IHttpRequestContextAccessor>();
                requests.Current.Returns(new AspNetCoreHttpRequestContext(context));
                var options = new ArcOptions();
                options.Tenancy.HttpHeader = "x-cratis-tenant-id";
                var resolver = new HeaderTenantIdResolver(requests, Options.Create(options));
                var tenants = new TenantIdAccessor(resolver);
                using var selectedTenant = tenants.Begin(resolver.Resolve());
                scenario.Services.AddSingleton<ITenantIdAccessor>(tenants);
                scenario.Services.AddArcAuthorizationPolicy<InviteMemberPolicy>(OnboardingPolicies.InviteMember);

                var store = Substitute.For<IEventStore>();
                store.GetEventSequence(EventSequenceId.Log).Returns(scenario.EventLog);
                var client = Substitute.For<IChronicleClient>();
                client.GetEventStore("Lending", Tenant).Returns(store);
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cratis:Chronicle:EventStore"] = "Lending"
                }).Build();
                using var cache = new MemoryCache(new MemoryCacheOptions());
                var transformation = new MemberPersonaClaims(client, new HttpContextAccessor { HttpContext = context }, cache, configuration);
                var principal = await transformation.TransformAsync(incoming);
                await client.Received(1).GetEventStore("Lending", Tenant);
                context.User = principal;
                using var caller = new CurrentPrincipalAccessor(new HttpRequestContextAccessor()).BeginScope(principal);

                var result = await scenario.Execute(new InviteMemberToTenant("invitee@example.com", ["Member"]));

                var stored = (await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
                var diagnostic = $"IsSuccess={result.IsSuccess}; IsAuthorized={result.IsAuthorized}; HasExceptions={result.HasExceptions}; ExceptionMessages={string.Join("; ", result.ExceptionMessages)}; AppendedEvents={scenario.AppendedEvents.Count()}; StoredEvents={stored.Length}";
                Assert.False(result.HasExceptions, diagnostic);
                Assert.False(result.IsSuccess, diagnostic);
                Assert.False(result.IsAuthorized, diagnostic);
                Assert.Empty(scenario.AppendedEvents);
                Assert.DoesNotContain(stored, appended => appended.Content is OnboardingMemberInvited);
                Assert.Single(stored);
            }

            static ClaimsPrincipal Incoming() => new(new ClaimsIdentity(
                [new Claim("urn:cratis:identity:provider-key", "EntraId"), new Claim("urn:cratis:identity:subject", "probe-caller")], "AuthProxy"));
        }
        #endif
        """;
}
