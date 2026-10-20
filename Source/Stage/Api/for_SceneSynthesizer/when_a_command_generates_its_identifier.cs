// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Forms;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

// A generated value is created when the command runs and is not entered: the form that creates the entity, and the toolbar dialog of its slice,
// ask for the supplier and never for the id.
public class when_a_command_generates_its_identifier : Specification
{
    const string Source = """
        concept PurchaseOrderId : Uuid
        concept SupplierId : Uuid
        module Buying
          feature Orders
            slice StateChange CreatePurchaseOrder
              command CreatePurchaseOrder
                purchaseOrderId PurchaseOrderId generated identifier
                supplierId SupplierId
                produces PurchaseOrderCreated
                  for purchaseOrderId
                  supplierId = supplierId
                returns purchaseOrderId
              event PurchaseOrderCreated
                supplierId SupplierId
        """;

    SceneApplication _result = null!;

    void Because()
    {
        var syntax = new ScreenplayCompiler().Compile(Source);
        syntax.Success.ShouldBeTrue();
        var scene = new ScreenplaySceneVisitor().Visit(syntax.Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Buying"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Buying", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var form = new ExternalComponent
        {
            Id = "create-form",
            Name = "create-form",
            ComponentName = "Cratis.Components.commandForm",
            Properties = new Dictionary<string, object?> { ["command"] = "CreatePurchaseOrder" },
            Slots = new Dictionary<string, IReadOnlyList<SceneElement>>()
        };
        var screen = new Screen(
            "NewPurchaseOrder",
            scene.Layouts[0].Name,
            new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal) { [DefaultLayout.ContentSlotName] = [form] },
            [new Form("CreatePurchaseOrderForm", "CreatePurchaseOrder", null, [], FormGenerationMode.Auto)],
            []);

        _result = SceneSynthesizer.Synthesize(scene with { Screens = [screen] }, compilation.Value!.Model);
    }

    ExternalComponent Placed => _result.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].OfType<ExternalComponent>().Single();

    [Fact] void should_not_ask_for_the_generated_identifier() => ((IReadOnlyList<FormField>)Placed.Properties["fields"]!).Select(field => field.Name).ShouldContainOnly(["supplierId"]);
    [Fact] void should_not_carry_the_generated_identifier_in_the_form_schema() => ((string)Placed.Properties[SceneSynthesizer.SchemaProperty]!).ShouldNotContain("purchaseOrderId");
    [Fact] void should_still_carry_what_the_person_enters() => ((string)Placed.Properties[SceneSynthesizer.SchemaProperty]!).ShouldContain("supplierId");
}
