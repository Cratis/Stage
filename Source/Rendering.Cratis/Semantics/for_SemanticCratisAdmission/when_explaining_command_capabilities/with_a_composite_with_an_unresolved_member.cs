// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_composite_with_an_unresolved_member : given.a_command_model
{
    void Establish() => SetModel(("type Details\n  display String\n" + Source).Replace("        produces InvoiceIssued", "        details Details\n        produces InvoiceIssued", StringComparison.Ordinal));

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var type = _model.Application.Types.Single();

        // Exercise the renderer boundary with a nested reference the executable-model compiler would reject.
        typeof(SemanticCompositeType).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(type, ImmutableArray.Create(type.Properties.Single() with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Unknown) }));
        var options = new CratisRenderingOptions("Invoices", "Invoices");
        var request = new ArtifactRenderRequest(_model, plan, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [context.Slice(_slice.Id)]);
    }

    [Fact] void should_not_report_a_command_capability_diagnostic() => _diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Artifact == _command.Id).ShouldBeEmpty();
    [Fact] void should_keep_the_unresolved_type_member_diagnostic() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-003" && diagnostic.Artifact == _model.Application.Types.Single().Id);
}
