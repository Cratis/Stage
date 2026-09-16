// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_malformed_project_options : a_register_project_render_request
{
    ArtifactRenderProfile[] _profiles = null!;
    ArtifactRenderPlan[] _plans = null!;

    void Establish()
    {
        var profile = CratisRendering.CreateProfile(_model.Application.Name, new("BackendHost", "Acme.projectAPI"));
        var project = profile.Inputs.Single(_ => _.Name.EndsWith(".csproj", StringComparison.Ordinal));
        var xml = Encoding.UTF8.GetString(project.Bytes.AsSpan());
        const string root = "<RootNamespace>Acme.projectAPI</RootNamespace>";
        _profiles =
        [
            ChangedProject("<Project>"),
            ChangedProject(xml.Replace(root, string.Empty, StringComparison.Ordinal)),
            ChangedProject(xml.Replace(root, $"{root}{root}", StringComparison.Ordinal)),
            ChangedProject(xml.Replace(root, "<RootNamespace>Acme..Projects</RootNamespace>", StringComparison.Ordinal)),
            ChangedProject(xml.Replace(root, "<RootNamespace>Acme.class</RootNamespace>", StringComparison.Ordinal)),
            ChangedProject(xml.Replace(root, "<RootNamespace> Acme.Projects </RootNamespace>", StringComparison.Ordinal)),
            ChangedProject(xml.Replace("</Project>", "<!-- tampered -->\n</Project>", StringComparison.Ordinal)),
            ReplaceProject(ArtifactRenderInput.Create(project.Name, project.Version, [0xff])),
            ReplaceProject(ArtifactRenderInput.Create("cratis-scaffold:text:Folder/BackendHost.csproj", project.Version, project.Bytes)),
            ReplaceProject(ArtifactRenderInput.Create("cratis-scaffold:text:OtherHost.csproj", project.Version, project.Bytes)),
            WithInputs([.. profile.Inputs, ArtifactRenderInput.Create("cratis-scaffold:text:OtherHost.csproj", project.Version, project.Bytes)])
        ];

        ArtifactRenderProfile ChangedProject(string content) => ReplaceProject(ArtifactRenderInput.Create(project.Name, project.Version, [.. Encoding.UTF8.GetBytes(content)]));
        ArtifactRenderProfile ReplaceProject(ArtifactRenderInput replacement) => WithInputs([.. profile.Inputs.Select(_ => _.Name == project.Name ? replacement : _)]);
        ArtifactRenderProfile WithInputs(System.Collections.Immutable.ImmutableArray<ArtifactRenderInput> inputs) => ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, inputs);
    }

    void Because() => _plans = [.. _profiles.Select(profile => _planner.Plan(_request with { Profile = profile }))];

    [Fact] void should_reject_every_malformed_or_tampered_profile() => _plans.All(_ => !_.Success).ShouldBeTrue();
    [Fact] void should_report_package_profile_rejection() => _plans.All(_ => _.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-CRATIS-001")).ShouldBeTrue();
    [Fact] void should_publish_no_candidate_artifacts() => _plans.SelectMany(_ => _.Artifacts).ShouldBeEmpty();
}
#endif
