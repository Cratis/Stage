// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

#pragma warning disable CA1507 // The strings are the keys of the configuration files, which happen to be the names of types.
namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

public partial class when_planning_authentication_with_signed_forwarded_identity : given.a_model_with_authentication_and_onboarding
{
    ArtifactRenderPlan _unsigned = null!;

    protected override bool SignedForwardedIdentity => true;

    JsonNode Arc => JsonNode.Parse(Text("appsettings.json"))!["Cratis"]!["Arc"]!;

    void Because() => _unsigned = PlanApplication(signedForwardedIdentity: false);

    [Fact] void should_plan_without_errors() => _plan.Diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_still_trust_the_identity_authproxy_forwards() => Arc["TrustForwardedIdentityHeaders"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_turn_signed_forwarded_identity_on() => Arc["ForwardedIdentity"]["Enabled"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_name_the_audience_after_the_application() => Arc["ForwardedIdentity"]["Audience"].GetValue<string>().ShouldEqual("lending-api");
    [Fact] void should_name_one_key_after_the_application() => Arc["ForwardedIdentity"]["Keys"].ToJsonString().ShouldEqual("[{\"KeyId\":\"lending-v1\"}]");
    [Fact] void should_not_render_a_secret_in_the_settings() => Text("appsettings.json").ShouldNotContain("SecretBase64");
    [Fact] void should_not_warn_that_the_forwarded_identity_is_unsigned() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-AUTH-003");
    [Fact] void should_list_the_arc_secret_as_a_secret() => Text(".runtime/environment.json").ShouldContain("\"key\": \"forwardedIdentity.arc.secretBase64\",\n      \"secret\": true");
    [Fact] void should_list_the_authproxy_secret_as_a_secret() => Text(".runtime/environment.json").ShouldContain("\"key\": \"forwardedIdentity.authProxy.secretBase64\",\n      \"secret\": true");
    [Fact] void should_keep_the_provider_keys() => Text(".runtime/environment.json").ShouldContain("authentication.providers.EntraId.clientSecret");
    [Fact] void should_not_render_a_secret_value_anywhere() => _plan.Artifacts.Any(artifact => Base64Secret.IsMatch(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ShouldBeFalse();

    // Only appsettings.json and the environment file change; everything else is the unsigned application byte for byte.
    [Fact] void should_not_sign_without_the_option() => Settings(_unsigned).ShouldNotContain("\"ForwardedIdentity\"");
    [Fact] void should_keep_every_other_file_byte_for_byte() => Describe(_plan).ShouldEqual(Describe(_unsigned));
    [Fact] void should_add_only_the_forwarded_identity_to_the_settings() => WithoutForwardedIdentity(Settings(_plan)).ShouldEqual(JsonNode.Parse(Settings(_unsigned))!.ToJsonString());

    static string Settings(ArtifactRenderPlan result) => Encoding.UTF8.GetString(result.Artifacts.Single(artifact => artifact.RelativePath == "appsettings.json").Bytes.AsSpan());

    static string WithoutForwardedIdentity(string settings)
    {
        var root = JsonNode.Parse(settings)!;
        root["Cratis"]!["Arc"]!.AsObject().Remove("ForwardedIdentity");
        return root.ToJsonString();
    }

    static string Describe(ArtifactRenderPlan result) =>
        string.Join('\n', result.Artifacts
            .Where(artifact => artifact.RelativePath is not "appsettings.json" and not ".runtime/environment.json")
            .Select(artifact => $"{artifact.RelativePath}:{Convert.ToHexString(artifact.Bytes.AsSpan())}"));

    [GeneratedRegex("[A-Za-z0-9+/]{43}=", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Base64Secret { get; }
}
