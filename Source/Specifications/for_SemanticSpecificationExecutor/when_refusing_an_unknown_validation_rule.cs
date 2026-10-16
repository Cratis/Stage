// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_an_unknown_validation_rule : given.a_command_only_plan
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_report_a_typed_refusal_without_running_the_specifications(bool conceptRule)
    {
        var model = _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => slice.Commands.Length > 0);
        var command = slice.Commands.Single();
        var concept = model.Application.Concepts[0];
        var rule = command.Validations[0] with { Kind = (SemanticValidationRuleKind)999 };
        var application = conceptRule
            ? model.Application with { Concepts = [concept with { Validations = [rule] }, .. model.Application.Concepts.Skip(1)] }
            : model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? candidate with { Commands = [command with { Validations = [rule] }] } : candidate)] }] }]
            };

        // Deliberately bypass compiler validation, as for a rule introduced by a newer dependency.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        var report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
        report.Results.ShouldNotBeEmpty();
        report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported && result.Unsupported!.Details.Contains("STAGE-ESM-005", StringComparison.Ordinal) && result.Unsupported.Details.Contains("999", StringComparison.Ordinal) && result.Unsupported.Construct == (conceptRule ? concept.Id : command.Id).ToString());
    }
}
