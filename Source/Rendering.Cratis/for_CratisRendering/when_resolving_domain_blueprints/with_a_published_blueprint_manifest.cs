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

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_resolving_domain_blueprints;

/// <summary>
/// An application whose ui profile activates a domain blueprint (<c>Cratis.Blueprint.Crm</c>) is renderable when the host approved the
/// blueprint's package, exactly as the npm package publishes its manifest, and is refused when it did not.
/// </summary>
public class with_a_published_blueprint_manifest : Specification
{
    // The manifest as @cratis/scene.blueprint.crm publishes it, including the design-time members and the dependency on the default blueprint.
    const string Document =
        "{ \"version\": 1, \"packages\": [{ \"manifest\": { \"name\": \"Cratis.Blueprint.Crm\", \"version\": \"1.0.0\", \"kind\": \"Blueprint\"," +
        " \"dependencies\": [{ \"name\": \"Cratis.Blueprint.Default\", \"versionRange\": \"^1.0.0\" }]," +
        " \"components\": [], \"layouts\": [], \"screenTemplates\": [\"CrmLeadList\"], \"dialogTemplates\": [\"CrmQuickCreateLead\"], \"themes\": []," +
        " \"displayName\": \"CRM\", \"module\": \"@cratis/scene.blueprint.crm\"," +
        " \"sampleData\": [{ \"id\": \"crm-nordic\", \"title\": \"Nordic CRM\", \"locale\": \"nb-NO\", \"path\": \"sampleData/crm-nordic.json\" }]," +
        " \"blueprint\": { \"domain\": \"crm\", \"description\": \"Leads.\", \"thumbnail\": \"thumbnails/crm.svg\"," +
        " \"screenSet\": [{ \"name\": \"crm.leads\", \"title\": \"Leads\", \"description\": \"Leads.\", \"template\": \"CrmLeadList\" }] } }," +
        " \"module\": { \"npm\": \"@cratis/scene.blueprint.crm\", \"version\": \"4.53.1\", \"export\": \"crmBlueprint\", \"styles\": [] } }] }";

    ApprovedPackageSetReadResult _read = null!;
    ApplicationRenderPlan _approved = null!;
    ApplicationRenderPlan _unapproved = null!;
    SceneApplication _application = null!;

    void Establish()
    {
        var layout = DefaultLayout.Create();
        var screen = new SceneScreens.Screen(
            "Leads",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                ["main"] = [SceneElementFactory.Component("Leads.main.0-action", "core:action"), SceneElementFactory.Component("Leads.main.1-button", "inputText")],
            },
            [],
            []);
        _application = new(
            [new SceneProfiles.UiProfile("Desktop", "web", ["Cratis.Components", "Cratis.Blueprint.Crm"], null, layout.Name, null)],
            [],
            [layout],
            [],
            [],
            [screen]);
    }

    void Because()
    {
        _read = ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames);
        _approved = RenderPlanner.Plan(_application, CratisRendering.CatalogWith(_read.Set));
        _unapproved = RenderPlanner.Plan(_application, CratisRendering.CatalogWith(null));
    }

    [Fact] void should_accept_the_manifest_as_published() => _read.Succeeded.ShouldBeTrue();
    [Fact] void should_resolve_the_application_when_the_blueprint_is_approved() => string.Join("; ", _approved.Targets.SelectMany(target => target.Findings).Select(finding => finding.Message)).ShouldEqual(string.Empty);
    [Fact] void should_activate_the_shipped_default_blueprint_the_blueprint_depends_on() => _approved.Targets[0].Profile.Packages.ShouldContain("Cratis.Blueprint.Default");
    [Fact] void should_activate_the_blueprint_after_the_package_it_builds_on() => _approved.Targets[0].Profile.Packages.ToList().IndexOf("Cratis.Blueprint.Crm").ShouldBeGreaterThan(_approved.Targets[0].Profile.Packages.ToList().IndexOf("Cratis.Blueprint.Default"));
    [Fact] void should_refuse_the_blueprint_when_it_was_not_approved() => _unapproved.Targets[0].Findings.Select(finding => finding.Kind).ShouldContain(RenderFindingKind.PackageNotInCatalog);
}
