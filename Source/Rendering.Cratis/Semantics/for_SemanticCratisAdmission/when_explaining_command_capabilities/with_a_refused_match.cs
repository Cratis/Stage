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

public class with_a_refused_match : given.a_command_model
{
    [Theory]
    [InlineData("b{10000}")]
    [InlineData("^(ab){10000}$")]
    [InlineData("a**")]
    [InlineData("(?=a)a")]
    public void should_carry_the_screenplay_admission_diagnostic(string pattern)
    {
        var execution = SemanticExecutionPlan.Compile(_model).Plan!;
        var rule = new SemanticValidationRule(_command.Properties.Single(property => property.Name == "description").Id, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
        typeof(SemanticCommand).GetField("<Validations>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_command, ImmutableArray.Create(rule));
        var rendered = CratisRendering.Plan(_model, execution, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("Invoices", "Invoices"));
        rendered.Success.ShouldBeFalse();
        rendered.Artifacts.ShouldBeEmpty();
        var check = PortableMatchPattern.Check(pattern);
        var diagnostic = rendered.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-005");
        diagnostic.Code.ShouldEqual("STAGE-ESM-005");
        diagnostic.Message.ShouldContain(check.Code!);
        diagnostic.Message.ShouldContain(check.Description);
    }
}
