// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public class when_planning_authentication : given.a_model_with_authentication_and_onboarding
{
    JsonNode AuthProxy => JsonNode.Parse(Text(".runtime/authproxy.application.json"))!;

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_list_every_provider_with_its_kind_and_identity() =>
        AuthProxy["Cratis"]!["AuthProxy"]!["Application"]!["Providers"]!.ToJsonString().ShouldEqual("[{\"Kind\":\"EntraId\",\"Identity\":\"EntraId\"},{\"Kind\":\"GitHub\",\"Identity\":\"GitHub\"},{\"Kind\":\"OpenId\",\"Identity\":\"Partner\"}]");
    [Fact] void should_tell_authproxy_the_application_has_onboarding() => AuthProxy["Cratis"]!["AuthProxy"]!["Application"]!["Onboarding"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_trust_the_identity_authproxy_forwards() => JsonNode.Parse(Text("appsettings.json"))!["Cratis"]!["Arc"]!["TrustForwardedIdentityHeaders"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_keep_the_scaffold_settings() => JsonNode.Parse(Text("appsettings.json"))!["Cratis"]!["Chronicle"]!["EventStore"]!.GetValue<string>().ShouldEqual(Application);
    [Fact] void should_read_the_runtime_conventions_before_cratis_does() => Text("Program.cs").IndexOf("RuntimeConventions.Apply(builder);", StringComparison.Ordinal).ShouldBeLessThan(Text("Program.cs").IndexOf("builder.AddCratis(", StringComparison.Ordinal));
    [Fact] void should_list_the_environment_keys_with_the_secrets_marked() => Text(".runtime/environment.json").ShouldContain("\"key\": \"authentication.providers.EntraId.clientSecret\",\n      \"secret\": true");
    [Fact] void should_list_the_tenant_of_an_entra_id_provider() => Text(".runtime/environment.json").ShouldContain("authentication.providers.EntraId.tenantId");
    [Fact] void should_list_the_authority_of_an_open_id_provider() => Text(".runtime/environment.json").ShouldContain("authentication.providers.Partner.authority");
    [Fact] void should_map_each_persona_to_the_roles_of_its_policies() => Text("GeneratedAuthentication/Personas.cs").ShouldContain("new(\"Librarian\", [\"Librarian\"], [], true)");
    [Fact] void should_map_a_persona_that_only_needs_a_sign_in_to_no_roles() => Text("GeneratedAuthentication/Personas.cs").ShouldContain("new(\"Visitor\", [], [], true)");
    [Fact] void should_still_protect_the_command_with_its_generated_policy() => _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("AddBook.cs", StringComparison.Ordinal)).Bytes.Length.ShouldBeGreaterThan(0);
    [Fact] void should_not_warn_that_providers_are_unsupported() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-AUTH-001");
}
