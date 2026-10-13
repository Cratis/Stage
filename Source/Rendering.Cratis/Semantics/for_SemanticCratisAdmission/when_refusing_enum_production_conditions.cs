// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_refusing_enum_production_conditions : Specification
{
    IEnumerable<ArtifactRenderDiagnostic> _diagnostics = [];

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(EnumMemberNumbersCorpus.V12.EsmBytes.AsSpan());
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile("Enums", new("Enums", "Enums")), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, new("Enums", "Enums"));
        var located = context.SelectedSlices().First(slice => slice.Slice.Commands.Any(command => command.Properties.Any(property =>
            property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty)));
        var command = located.Slice.Commands.First(command => command.Properties.Any(property =>
            property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty));
        var property = command.Properties.First(property => property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty);
        var condition = new SemanticComparison(new(property.Id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text(context.Concepts[property.Type.Target].Values[0])));
        var changed = command with { Produces = [.. command.Produces.Select(produced => produced with { When = condition })] };
        var slice = located.Slice with { Commands = [.. located.Slice.Commands.Select(existing => existing.Id == command.Id ? changed : existing)] };
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice }]);
    }

    [Fact] void should_refuse_enum_equality() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-006");
}
