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
/// A screen that fills a template of a domain blueprint ('template blueprint Cratis.Blueprint.Crm CrmLeadList') renders when the approved
/// blueprint's manifest lists that template, and is refused - with the blueprint and the template named - when it does not, instead of
/// rendering a screen that silently has no template (Cratis/Stage#457).
/// </summary>
public class with_template_references_to_a_published_blueprint : Specification
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

    ApplicationRenderPlan _plan = null!;
    ApplicationRenderPlan _listed = null!;

    static SceneApplication ApplicationReferencing(params string[] templates)
    {
        var layout = DefaultLayout.Create();
        return new(
            [new SceneProfiles.UiProfile("Desktop", "web", ["Cratis.Components", "Cratis.Blueprint.Crm"], null, layout.Name, null)],
            [],
            [layout],
            [],
            [],
            [])
        {
            TemplateReferences = [.. templates.Select(template => new TemplateAssignment("screen", $"Crm.Leads.{template}", new(SceneScreens.TemplateReferenceSource.Blueprint, template, "Cratis.Blueprint.Crm", "^1.0.0")))]
        };
    }

    void Because()
    {
        var approved = CratisRendering.CatalogWith(ApprovedPackageSetJson.Read(Document, CratisRendering.ShippedPackageNames).Set);
        _listed = RenderPlanner.Plan(ApplicationReferencing("CrmLeadList", "CrmQuickCreateLead"), approved);
        _plan = RenderPlanner.Plan(ApplicationReferencing("CrmLeadList", "CrmActivityCreateForm"), approved);
    }

    IEnumerable<RenderFinding> Unresolved => _plan.Findings.Where(finding => finding.Kind == RenderFindingKind.TemplateReferenceUnresolved);

    [Fact] void should_resolve_the_templates_the_manifest_lists() => _listed.Findings.Where(finding => finding.Kind == RenderFindingKind.TemplateReferenceUnresolved).ShouldBeEmpty();
    [Fact] void should_refuse_exactly_the_template_it_does_not_list() => Unresolved.Count().ShouldEqual(1);
    [Fact] void should_name_the_blueprint_and_the_template() => Unresolved.Single().Message.ShouldEqual("Blueprint 'Cratis.Blueprint.Crm' has no template 'CrmActivityCreateForm'");
    [Fact] void should_say_where_it_was_referenced() => Unresolved.Single().Subject.ShouldEqual("Crm.Leads.CrmActivityCreateForm");
}
