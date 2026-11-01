// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Conformance.Specs.for_RenderOutcomeContract.given;

public class outcome_vectors : Specification
{
    protected ArtifactRenderRequest _request = null!;

    void Establish()
    {
        var identity = ApplicationIdentity.Create("OutcomeVectors");
        var application = new SemanticApplication(SemanticId.Create(SemanticAddress.ForApplication(identity)), "OutcomeVectors", [], [], []);
        var model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application);
        _request = new(model, SemanticExecutionPlan.Compile(model).Plan!, ArtifactRenderProfile.Create("contract", "1", "vectors", "1", []), new(ArtifactRenderScopeKind.Application, application.Id));
    }

    protected static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value.ToString("x64", CultureInfo.InvariantCulture)}");

    protected static SemanticAddress Locator(int value) => SemanticAddress.ForConcept(ApplicationIdentity.Create("OutcomeVectors"), $"Owner{value.ToString(CultureInfo.InvariantCulture)}");

    protected static ArtifactRenderConstruct Degraded(int id, string code, ImmutableArray<ArtifactRenderGapReference> roots = default)
    {
        var warning = new ArtifactRenderDiagnostic(code, ArtifactRenderDiagnosticSeverity.Warning, "Retain the described contract.", Id(id));
        var gap = new ArtifactRenderConstructGap(new(warning, "Implement and verify."), ArtifactRenderPlaceholderKind.RetainedSignatureOrType, warning.Message, "model/outcomes.play")
        {
            SemanticLocator = Locator(id),
            Roots = roots.IsDefault ? [] : roots
        };
        return new(Id(id), ArtifactRenderConstructKind.CompositeType, "model/outcomes.play", ArtifactRenderOutcome.Degraded) { SemanticLocator = Locator(id), Degradations = [gap] };
    }

    protected ArtifactRenderPlan Plan(ImmutableArray<ArtifactRenderConstruct> constructs, bool retain = true)
    {
        var inventory = ArtifactRenderInventory.Create(constructs);
        var artifacts = retain ? constructs.Where(construct => construct.Outcome != ArtifactRenderOutcome.Refused)
            .Select((construct, index) => PlannedArtifact.CreateText($"Owner{index.ToString(CultureInfo.InvariantCulture)}.cs", "// Retained signature\npublic interface IContract {}", [construct.Identity])).ToImmutableArray() : [];
        var diagnostics = constructs.SelectMany(construct => construct.Degradations.Select(gap => gap.Evidence.Diagnostic).Concat(construct.RefusalReasons)).ToImmutableArray();
        return ArtifactRenderPlan.Create(_request, artifacts, diagnostics).WithInventory(inventory);
    }
}
