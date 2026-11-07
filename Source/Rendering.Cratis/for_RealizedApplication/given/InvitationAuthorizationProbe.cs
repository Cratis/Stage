// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication.given;

/// <summary>
/// Exercises tenant persona enrichment before dispatching the generated invitation command.
/// </summary>
public static class InvitationAuthorizationProbe
{
    /// <summary>
    /// Gets the in-application authorization probe and its provisioned control.
    /// </summary>
    public const string Source = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Security.Claims;
        using System.Text.Json;
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
        using Cratis.Showrunner.BoxOffice.Gating;
        using Lending.GeneratedAuthentication;
        using Lending.GeneratedOnboarding;
        using Microsoft.AspNetCore.Http;
        using Microsoft.Extensions.Caching.Memory;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Options;
        using NSubstitute;
        using Xunit;
        using Xunit.Abstractions;

        namespace Lending.OnboardingAuthorizationProbe;

        public class when_inviting_a_member(ITestOutputHelper output)
        {
            const string Tenant = "tenant-b";

            [Fact]
            public Task should_deny_an_anonymous_caller() => Run(authenticated: false);

            [Fact]
            public Task should_deny_a_member_who_may_not_invite() => Run(provisionedPersonas: ["Member"]);

            [Fact]
            public Task should_refuse_an_inviter_granting_librarian() => Run(provisionedPersonas: ["Librarian"], grantedPersonas: ["Librarian"]);

            [Fact]
            public Task should_deny_an_idp_librarian_without_a_persona_in_the_selected_tenant() => Run(forwardedLibrarian: true);

            [Fact]
            public Task should_deny_an_inviter_of_another_tenant() => Run(provisionedPersonas: ["Librarian"], provisionedTenant: "tenant-a", forwardedLibrarian: true);

            [Fact]
            public Task should_allow_a_librarian_provisioned_in_the_selected_tenant() => Run(provisionedPersonas: ["Librarian"]);

            async Task Run(bool authenticated = true, string[]? provisionedPersonas = null, string provisionedTenant = Tenant, bool forwardedLibrarian = false, string[]? grantedPersonas = null)
            {
                using var scenario = new CommandScenario<InviteMemberToTenant>();
                using var otherTenant = new CommandScenario<InviteMemberToTenant>();
                var claims = new List<Claim>
                {
                    new("urn:cratis:identity:provider-key", "EntraId"),
                    new("urn:cratis:identity:subject", "probe-caller")
                };
                if (forwardedLibrarian)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Librarian"));
                }
                var incoming = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "AuthProxy" : null));
                var member = Assert.IsType<MemberIdentity>(MemberIdentity.From(incoming));
                var provisionedHere = provisionedPersonas is not null && provisionedTenant == Tenant;
                if (provisionedPersonas is not null)
                {
                    var seeded = provisionedHere ? scenario : otherTenant;
                    seeded.Given.ForEventSource(member.Key).Events(new OnboardingMemberProvisioned(member.Provider, member.Subject, provisionedPersonas));
                }

                var context = new DefaultHttpContext { User = incoming };
                context.Request.Headers["x-cratis-tenant-id"] = Tenant;
                var requests = Substitute.For<IHttpRequestContextAccessor>();
                requests.Current.Returns(new AspNetCoreHttpRequestContext(context));
                var options = new ArcOptions();
                options.Tenancy.HttpHeader = "x-cratis-tenant-id";
                var resolver = new HeaderTenantIdResolver(requests, Options.Create(options));
                var tenants = new TenantIdAccessor(resolver);
                using var selectedTenant = tenants.Begin(resolver.Resolve());
                Assert.Equal(Tenant, tenants.Current.Value);
                scenario.Services.AddSingleton<ITenantIdAccessor>(tenants);
                scenario.Services.AddArcAuthorizationPolicy<InviteMemberPolicy>(OnboardingPolicies.InviteMember);

                // The model's offering puts Box Office's module gate in the pipeline; entitlements are not under test here.
                var gate = Substitute.For<IModuleGate>();
                gate.Admits(Arg.Any<object>()).Returns(true);
                scenario.Services.AddSingleton(gate);

                // Only the Chronicle transport is substituted: provisioning reads the real scenario logs.
                var store = Substitute.For<IEventStore>();
                store.GetEventSequence(EventSequenceId.Log).Returns(scenario.EventLog);
                var otherStore = Substitute.For<IEventStore>();
                otherStore.GetEventSequence(EventSequenceId.Log).Returns(otherTenant.EventLog);
                var client = Substitute.For<IChronicleClient>();
                client.GetEventStore("Lending", Tenant).Returns(store);
                client.GetEventStore("Lending", "tenant-a").Returns(otherStore);
                var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cratis:Chronicle:EventStore"] = "Lending"
                }).Build();
                using var cache = new MemoryCache(new MemoryCacheOptions());
                var transformation = new MemberPersonaClaims(client, new HttpContextAccessor { HttpContext = context }, cache, configuration);
                var principal = await transformation.TransformAsync(incoming);
                if (authenticated)
                {
                    await client.Received(1).GetEventStore("Lending", Tenant);
                }
                context.User = principal;
                using var caller = new CurrentPrincipalAccessor(new HttpRequestContextAccessor()).BeginScope(principal);
                var result = await scenario.Execute(new InviteMemberToTenant("invitee@example.com", grantedPersonas ?? ["Member"]));
                var stored = (await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
                var diagnostic = $"ProvisionedTenant={provisionedTenant}; IsSuccess={result.IsSuccess}; IsAuthorized={result.IsAuthorized}; IsValid={result.IsValid}; HasExceptions={result.HasExceptions}; AuthorizationFailureReason={result.AuthorizationFailureReason}; ValidationResults={JsonSerializer.Serialize(result.ValidationResults)}; ExceptionMessages={string.Join("; ", result.ExceptionMessages)}; AppendedEvents={scenario.AppendedEvents.Count()}; StoredEvents={stored.Length}";
                output.WriteLine(diagnostic);
                Assert.False(result.HasExceptions, diagnostic);
                Assert.Empty(result.ExceptionMessages);
                var inviter = provisionedHere && provisionedPersonas!.Contains("Librarian");
                if (inviter && grantedPersonas is null)
                {
                    Assert.True(result.IsSuccess, diagnostic);
                    Assert.Single(scenario.AppendedEvents);
                    var invited = Assert.IsType<OnboardingMemberInvited>(Assert.Single(stored, appended => appended.Content is OnboardingMemberInvited).Content);
                    Assert.Equal("invitee@example.com", invited.Email);
                    Assert.Equal(Tenant, invited.Tenant);
                    Assert.Equal(new[] { "Member" }, invited.Personas);
                }
                else
                {
                    Assert.False(result.IsSuccess, diagnostic);
                    if (inviter)
                    {
                        Assert.True(result.IsAuthorized, diagnostic);
                        Assert.False(result.IsValid, diagnostic);
                    }
                    else
                    {
                        Assert.False(result.IsAuthorized, diagnostic);
                    }
                    Assert.Empty(scenario.AppendedEvents);
                    Assert.DoesNotContain(stored, appended => appended.Content is OnboardingMemberInvited);
                    Assert.Equal(provisionedHere ? 1 : 0, stored.Length);
                }
            }
        }
        #endif
        """;
}
