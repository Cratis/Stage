// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_a_strict_scaffold_with_user_interface_domains : Specification
{
    const string InvalidDomains = "The Cratis profile carries an invalid, duplicate, reserved or unaccompanied user interface domains input.";
    CratisPlanOptions _options = null!;
    ArtifactRenderProfile _profile = null!;
    CratisPlanResult _baseline = null!;
    CratisPlanResult _result = null!;

    void Establish()
    {
        _options = new("Shop", "Shop.Backend", "Acme.Shop");
        _profile = CratisRendering.CreateProfile(_options.ApplicationName, new(_options.ProjectName, _options.RootNamespace) { Strict = true });
        _baseline = CratisRendering.PlanScaffold(_options with { Strict = true });
    }

    void Because() => _result = Plan(UserInterface(), Domains());

    [Fact]
    void should_plan_the_public_strict_scaffold_with_accompanied_domains()
    {
        _result.Success.ShouldBeTrue();
        Assert.NotEmpty(_result.Artifacts);
        Assert.Empty(_result.Diagnostics);
    }

    [Fact]
    void should_preserve_the_scaffold_roster_without_emitting_protocol_metadata() =>
        _result.Artifacts.Select(artifact => artifact.RelativePath).ShouldEqual(_baseline.Artifacts.Select(artifact => artifact.RelativePath));

    [Fact]
    void should_preserve_scaffold_bytes_and_diagnostics() => _result.Digest.ShouldEqual(_baseline.Digest);

    [Theory]
    [InlineData("1", "{")]
    [InlineData("1", "{\"../Unsafe\":[\"screen\"]}")]
    [InlineData("1", "{\"Customizations\":[\"screen\"]}")]
    [InlineData("1", "{\"Sales\":[\"screen\"],\"Sales\":[\"screen\"]}")]
    [InlineData("2", "{\"Sales\":[\"screen\"]}")]
    void should_refuse_malformed_or_reserved_domains(string version, string text) =>
        AssertRefused(Plan(UserInterface(), Input(UserInterfaceDomainsInput.Name, version, text)), InvalidDomains);

    [Fact]
    void should_refuse_duplicate_domains() =>
        AssertRefused(Plan(UserInterface(), Domains(), Input(UserInterfaceDomainsInput.Name, "2", "{\"Sales\":[\"screen\"]}")), InvalidDomains);

    [Fact]
    void should_refuse_unaccompanied_domains() => AssertRefused(Plan(Domains()), InvalidDomains);

    [Theory]
    [InlineData("stage:unknown")]
    [InlineData("cratis-render:unknown")]
    [InlineData(UserInterfaceDomainsInput.Name + ":unknown")]
    [InlineData("CRATIS-RENDER:USER-INTERFACE-DOMAINS")]
    void should_refuse_unknown_inputs_even_with_valid_strict_acceptance_and_domains(string name) =>
        AssertRefused(Plan(UserInterface(), Domains(), Input(name, "1", "{}")), "The Cratis profile scaffold roster, versions, bytes, or SHA-256 hashes do not match the package-owned profile.");

    CratisPlanResult Plan(params ArtifactRenderInput[] inputs) => CratisRendering.PlanScaffold(_options with
    {
        Profile = ArtifactRenderProfile.Create(_profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs, .. inputs])
    });

    static ArtifactRenderInput UserInterface() => UserInterfaceInput.Create(["screen"]);
    static ArtifactRenderInput Domains() => UserInterfaceDomainsInput.Create([KeyValuePair.Create("Sales", (IEnumerable<string>)["screen"])]);
    static ArtifactRenderInput Input(string name, string version, string text) => ArtifactRenderInput.Create(name, version, [.. Encoding.UTF8.GetBytes(text)]);

    static void AssertRefused(CratisPlanResult result, string expected)
    {
        result.Success.ShouldBeFalse();
        Assert.Empty(result.Artifacts);
        var diagnostic = Assert.Single(result.Diagnostics);
        diagnostic.Code.ShouldEqual("STAGE-CRATIS-001");
        Assert.EndsWith(expected, diagnostic.Message, StringComparison.Ordinal);
    }
}
