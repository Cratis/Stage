// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory.given;

public class an_inventory : for_ArtifactRenderPlan.given.an_artifact_render_request
{
    protected ArtifactRenderConstruct _root = null!;
    protected ArtifactRenderConstruct _middle = null!;
    protected ArtifactRenderConstruct _leaf = null!;
    protected ArtifactRenderConstruct _rendered = null!;
    protected ArtifactRenderInventory _inventory = null!;
    protected ArtifactRenderPlan _plan = null!;
    protected ArtifactRenderReportContext _context = null!;

    void Establish()
    {
        _root = Degraded(1, ArtifactRenderConstructKind.Command, "STAGE-DEGRADE-002", []);
        _middle = Degraded(2, ArtifactRenderConstructKind.Query, "STAGE-DEGRADE-005", [new(_root.Identity, "STAGE-DEGRADE-002")]);
        _leaf = Degraded(3, ArtifactRenderConstructKind.ReadModel, "STAGE-DEGRADE-005", [new(_middle.Identity, "STAGE-DEGRADE-005")]);
        _rendered = new(Id(4), ArtifactRenderConstructKind.Event, "model/orders.play", ArtifactRenderOutcome.Rendered) { SemanticLocator = Locator(4) };
        _inventory = ArtifactRenderInventory.Create([_leaf, _middle, _rendered, _root]);
        _plan = Plan(_inventory);
        var pair = new ArtifactRenderVersionPair(LanguageVersion.V1, SemanticVersion.V1);
        _context = new(pair, new([pair]), [new("Example.Pack", "1.2.3")]);
    }

    protected static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value.ToString("x64", CultureInfo.InvariantCulture)}");

    protected static SemanticAddress Locator(int value) => SemanticAddress.ForConcept(ApplicationIdentity.Create("Projects"), $"Owner{value.ToString(CultureInfo.InvariantCulture)}");

    protected static ArtifactRenderConstruct Degraded(int id, ArtifactRenderConstructKind kind, string code, ImmutableArray<ArtifactRenderGapReference> roots)
    {
        var warning = new ArtifactRenderDiagnostic(code, ArtifactRenderDiagnosticSeverity.Warning, "Café e\u0301 requirement", Id(id));
        var gap = new ArtifactRenderConstructGap(new(warning, "Implement and verify."), ArtifactRenderPlaceholderKind.ThrowingOperationBody, warning.Message, "model/orders.play") { Roots = roots, SemanticLocator = Locator(id) };
        return new(Id(id), kind, "model/orders.play", ArtifactRenderOutcome.Degraded) { Degradations = [gap], SemanticLocator = Locator(id) };
    }

    protected ArtifactRenderPlan Plan(ArtifactRenderInventory inventory) => ArtifactRenderPlan.Create(
        _request,
        [],
        [.. inventory.Constructs.SelectMany(construct => construct.Degradations.Select(gap => gap.Evidence.Diagnostic)).Concat(inventory.Constructs.SelectMany(construct => construct.RefusalReasons))]).WithInventory(inventory);
}
