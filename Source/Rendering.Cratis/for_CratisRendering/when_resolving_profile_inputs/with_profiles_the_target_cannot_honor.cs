// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneProfiles = Cratis.Scene.Model.Profiles;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_resolving_profile_inputs;

/// <summary>
/// The profile inputs every renderer consumer refuses: a package nobody approved, a layout that does not exist and a profile that
/// resolves. One rule serves the CLI and Studio, so neither holds a second answer.
/// </summary>
public class with_profiles_the_target_cannot_honor : Specification
{
    IReadOnlyList<CratisProfileInputFinding> _valid = [];
    IReadOnlyList<CratisProfileInputFinding> _unknownPackage = [];
    IReadOnlyList<CratisProfileInputFinding> _missingLayout = [];

    static SceneApplication ApplicationWith(string[] packages, string? layoutName = null)
    {
        var layout = DefaultLayout.Create();
        var screen = new SceneScreens.Screen(
            "Leads",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                ["main"] = [SceneElementFactory.Component("Leads.main.0-action", "core:action")],
            },
            [],
            []);
        return new(
            [new SceneProfiles.UiProfile("Desktop", "web", packages, null, layoutName ?? layout.Name, null)],
            [],
            [layout],
            [],
            [],
            [screen]);
    }

    void Because()
    {
        _valid = CratisRendering.ResolveProfileInputs(ApplicationWith(["Cratis.Components"]));
        _unknownPackage = CratisRendering.ResolveProfileInputs(ApplicationWith(["Cratis.Components", "Cratis.NotAPackage"]));
        _missingLayout = CratisRendering.ResolveProfileInputs(ApplicationWith(["Cratis.Components"], "MissingShell"));
    }

    [Fact] void should_report_nothing_for_a_profile_that_resolves() => _valid.ShouldBeEmpty();
    [Fact] void should_report_the_package_nobody_approved() => _unknownPackage.Select(finding => finding.Code).ShouldContain(CratisRendering.UnknownPackageCode);
    [Fact] void should_name_the_unknown_package() => _unknownPackage.Select(finding => finding.Subject).ShouldContain("Cratis.NotAPackage");
    [Fact] void should_report_the_kind_the_scene_engine_found() => _unknownPackage.Select(finding => finding.Kind).ShouldContain(RenderFindingKind.PackageNotInCatalog);
    [Fact] void should_report_the_layout_that_does_not_exist() => _missingLayout.Select(finding => finding.Code).ShouldContain(CratisRendering.MissingLayoutCode);
}
