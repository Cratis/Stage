// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderProfileAdmission;

public class when_admitting_strict_acceptance_with_user_interface_domains : Specification
{
    const string InvalidDomains = "The Cratis profile carries an invalid, duplicate, reserved or unaccompanied user interface domains input.";
    ArtifactRenderProfile _profile = null!;

    void Establish()
    {
        var options = new CratisRenderingOptions("Shop", "Shop");
        _profile = CratisRendering.CreateProfile("Shop", options);
    }

    [Fact]
    void should_admit_valid_strict_acceptance_and_accompanied_domains_together()
    {
        var profile = WithInputs(UserInterface(), Domains(), StrictAcceptanceInput.Create(true));
        CratisArtifactRenderProfileAdmission.MatchesScaffold(profile, "Shop", out var options, out var mismatch).ShouldBeTrue();
        options!.Strict.ShouldBeTrue();
        mismatch.ShouldEqual(string.Empty);
        UserInterfaceDomainsInput.TryRead(profile, out var domains).ShouldBeTrue();
        Assert.Equal("screen", Assert.Single(domains!["Sales"]));
    }

    [Theory]
    [InlineData("1", "TRUE")]
    [InlineData("1", "true\n")]
    [InlineData("2", "true")]
    void should_refuse_malformed_strict_input_even_with_valid_domains(string version, string text)
    {
        var profile = WithInputs(UserInterface(), Domains(), Input(StrictAcceptanceInput.Name, version, text));
        AssertRefused(profile, CratisStrictProfileInput.InvalidInput);
    }

    [Fact]
    void should_refuse_duplicate_strict_inputs_even_with_valid_domains()
    {
        var profile = WithInputs(UserInterface(), Domains(), StrictAcceptanceInput.Create(true), Input(StrictAcceptanceInput.Name, "2", "true"));
        AssertRefused(profile, CratisStrictProfileInput.InvalidInput);
    }

    [Theory]
    [InlineData("1", "{")]
    [InlineData("1", "{\"../Unsafe\":[\"screen\"]}")]
    [InlineData("1", "{\"Customizations\":[\"screen\"]}")]
    [InlineData("2", "{\"Sales\":[\"screen\"]}")]
    void should_refuse_invalid_or_reserved_domains_before_admitting_strict_input(string version, string text)
    {
        var profile = WithInputs(UserInterface(), Input(UserInterfaceDomainsInput.Name, version, text), StrictAcceptanceInput.Create(true));
        AssertRefused(profile, InvalidDomains);
    }

    [Fact]
    void should_refuse_duplicate_domains_even_with_valid_strict_input()
    {
        var profile = WithInputs(UserInterface(), Domains(), Input(UserInterfaceDomainsInput.Name, "2", "{\"Sales\":[\"screen\"]}"), StrictAcceptanceInput.Create(true));
        AssertRefused(profile, InvalidDomains);
    }

    [Fact]
    void should_refuse_unaccompanied_domains_even_with_valid_strict_input()
    {
        var profile = WithInputs(Domains(), StrictAcceptanceInput.Create(true));
        AssertRefused(profile, InvalidDomains);
    }

    ArtifactRenderProfile WithInputs(params ArtifactRenderInput[] inputs) => ArtifactRenderProfile.Create(
        _profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs, .. inputs]);

    static ArtifactRenderInput UserInterface() => UserInterfaceInput.Create(["screen"]);
    static ArtifactRenderInput Domains() => UserInterfaceDomainsInput.Create([KeyValuePair.Create("Sales", (IEnumerable<string>)["screen"])]);
    static ArtifactRenderInput Input(string name, string version, string text) => ArtifactRenderInput.Create(name, version, [.. Encoding.UTF8.GetBytes(text)]);

    static void AssertRefused(ArtifactRenderProfile profile, string expected)
    {
        CratisArtifactRenderProfileAdmission.MatchesScaffold(profile, "Shop", out var options, out var mismatch).ShouldBeFalse();
        Assert.Null(options);
        mismatch.ShouldEqual(expected);
    }
}
