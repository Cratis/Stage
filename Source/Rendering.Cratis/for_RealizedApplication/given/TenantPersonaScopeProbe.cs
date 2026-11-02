// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication.given;

/// <summary>
/// Exercises tenant-scoped persona identities in the generated application.
/// </summary>
public static class TenantPersonaScopeProbe
{
    /// <summary>
    /// Gets the in-application persona scoping specifications.
    /// </summary>
    public const string Source = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using System.Security.Claims;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Tenancy;
        using Lending.GeneratedOnboarding;
        using NSubstitute;
        using Xunit;

        namespace Lending.OnboardingAuthorizationProbe;

        public class when_scoping_member_personas
        {
            const string Tenant = "tenant-b";

            [Fact]
            public void should_remove_persona_roles_without_a_tenant_and_keep_other_roles()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), null, ["Librarian"]);
                Assert.False(principal.IsInRole("Librarian"));
                Assert.True(principal.IsInRole("Auditor"));
                Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == MemberPersonaClaims.AuthenticationType);
            }

            [Fact]
            public void should_add_no_persona_identity_without_a_grant()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), Tenant, []);
                Assert.False(principal.IsInRole("Librarian"));
                Assert.DoesNotContain(principal.Identities, identity => identity.AuthenticationType == MemberPersonaClaims.AuthenticationType);
            }

            [Fact]
            public void should_grant_only_the_tenants_member_persona()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), Tenant, ["Member"]);
                Assert.True(principal.IsInRole("Member"));
                Assert.False(principal.IsInRole("Librarian"));
                var granted = Assert.Single(principal.Identities, identity => identity.AuthenticationType == MemberPersonaClaims.AuthenticationType);
                Assert.True(granted.HasClaim(MemberPersonaClaims.TenantClaimType, Tenant));
                Assert.Equal("Member", Assert.Single(granted.FindAll(MemberPersonaClaims.PersonaClaimType)).Value);
                Assert.Equal("AuthProxy", principal.Identity!.AuthenticationType);
            }

            [Fact]
            public void should_remove_forwarded_tenant_and_persona_claims_from_every_identity()
            {
                var incoming = Incoming();
                incoming.AddIdentity(new ClaimsIdentity(
                    [new Claim(MemberPersonaClaims.TenantClaimType, Tenant), new Claim(MemberPersonaClaims.PersonaClaimType, "Librarian"), new Claim("groups", "Librarian")],
                    "Partner", ClaimTypes.Name, "groups"));
                var principal = MemberPersonaClaims.Scope(incoming, Tenant, []);
                Assert.DoesNotContain(principal.Claims, claim => claim.Type == MemberPersonaClaims.TenantClaimType || claim.Type == MemberPersonaClaims.PersonaClaimType);
                Assert.False(principal.IsInRole("Librarian"));
                Assert.Equal(new[] { "AuthProxy", "Partner" }, principal.Identities.Select(identity => identity.AuthenticationType));
            }

            [Fact]
            public void should_replace_the_previous_persona_identity_on_repeated_scoping()
            {
                var once = MemberPersonaClaims.Scope(Incoming(), Tenant, ["Member"]);
                var twice = MemberPersonaClaims.Scope(once, Tenant, ["Member"]);
                Assert.Equal(once.Claims.Select(claim => (claim.Type, claim.Value)), twice.Claims.Select(claim => (claim.Type, claim.Value)));
                Assert.Single(twice.Identities, identity => identity.AuthenticationType == MemberPersonaClaims.AuthenticationType);
            }

            [Fact]
            public void should_leave_the_incoming_principal_unchanged()
            {
                var incoming = Incoming();
                var before = incoming.Claims.Select(claim => (claim.Type, claim.Value)).ToArray();
                var principal = MemberPersonaClaims.Scope(incoming, Tenant, ["Member"]);
                Assert.NotSame(incoming, principal);
                Assert.NotSame(incoming.Identity, principal.Identity);
                Assert.Equal(before, incoming.Claims.Select(claim => (claim.Type, claim.Value)));
                Assert.True(incoming.IsInRole("Librarian"));
                Assert.Single(incoming.Identities);
            }

            [Fact]
            public async Task should_authorize_only_in_the_tenant_of_the_invitation()
            {
                var principal = MemberPersonaClaims.Scope(Incoming(), "tenant-a", ["Librarian"]);
                Assert.True(MemberPersonaClaims.Holds(principal, "tenant-a", "Librarian"));
                Assert.False(await Policy().IsAuthorized(new AuthorizationPolicyContext(principal, typeof(InviteMemberToTenant), new InviteMemberToTenant("invitee@example.com", ["Member"])), CancellationToken.None));
            }

            [Fact]
            public async Task should_require_a_tenant_persona_identity_rather_than_a_role_alone() =>
                Assert.False(await Policy().IsAuthorized(new AuthorizationPolicyContext(Incoming(), typeof(InviteMemberToTenant), new InviteMemberToTenant("invitee@example.com", ["Member"])), CancellationToken.None));

            static ClaimsPrincipal Incoming() => new(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Librarian"), new Claim(ClaimTypes.Role, "Auditor")], "AuthProxy"));

            static InviteMemberPolicy Policy()
            {
                var tenants = Substitute.For<ITenantIdAccessor>();
                tenants.Current.Returns(new TenantId(Tenant));

                return new InviteMemberPolicy(tenants);
            }
        }
        #endif
        """;
}
