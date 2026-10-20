// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Packages;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_roots_on_a_blueprint_shell : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _satisfied = null!;
    ApplicationRenderPlan _unsatisfied = null!;
    ApplicationRenderPlan _notInstalled = null!;

    void Establish() => _catalog =
    [
        .. _catalog,
        new ScenePackage("Cratis.Blueprint.Default", "4.75.0", PackageKind.Blueprint, [], [], ["AppShell", "FullPage"], [], [], []),
    ];

    void Because()
    {
        _satisfied = RenderPlanner.Plan(Application("^4.5", "Cratis.Blueprint.Default"), _catalog);
        _unsatisfied = RenderPlanner.Plan(Application("^5.0", "Cratis.Blueprint.Default"), _catalog);
        _notInstalled = RenderPlanner.Plan(Application("^4.5", "Cratis.Blueprint.Default", installed: false), _catalog);
    }

    SceneApplication Application(string version, string blueprint, bool installed = true) => _application with
    {
        UiProfiles =
        [
            new UiProfile(
                "Admin",
                "web",
                installed ? ["PrimeReact", blueprint] : ["PrimeReact"],
                null,
                null,
                null,
                Root: new RootTemplate(new TemplateReference(TemplateReferenceSource.Blueprint, "AppShell", blueprint, version))),
        ],
    };

    [Fact] void should_resolve_the_blueprint_shell_when_the_installed_version_satisfies_the_pin() => _satisfied.Targets[0].Shell!.Package.ShouldEqual("Cratis.Blueprint.Default");
    [Fact] void should_have_no_findings_when_the_pin_is_satisfied() => _satisfied.Targets[0].Findings.ShouldBeEmpty();
    [Fact] void should_name_the_shell_without_inventing_its_structure() => _satisfied.Targets[0].Layout.ShouldBeNull();
    [Fact] void should_fail_the_build_when_the_pin_is_not_satisfied() => _unsatisfied.Targets[0].Findings.Any(finding => finding.Kind == RenderFindingKind.RootTemplateInvalid).ShouldBeTrue();
    [Fact] void should_say_which_version_is_installed() => _unsatisfied.Targets[0].Findings.Single(finding => finding.Kind == RenderFindingKind.RootTemplateInvalid).Message.ShouldContain("4.75.0");
    [Fact] void should_fail_the_build_when_the_blueprint_is_not_among_the_profile_s_packages() => _notInstalled.Targets[0].Findings.Any(finding => finding.Kind == RenderFindingKind.RootTemplateInvalid).ShouldBeTrue();
}
