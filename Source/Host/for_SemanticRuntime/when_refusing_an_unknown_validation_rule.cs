// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_refusing_an_unknown_validation_rule : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_block_registration_with_the_unsupported_validation_code(bool conceptRule)
    {
        var model = SemanticModelSerializer.Deserialize(RegisterProjectCorpus.LegacyV1.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
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

        // Fault injection bypasses Screenplay validation so this tests Stage's own fail-closed boundary.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        var admission = new SemanticRuntimeAdmission(plan);
        Assert.Contains(admission.Blocking, entry => entry.Artifact == (conceptRule ? concept.Id : command.Id).ToString() && entry.Details!.Contains("STAGE-ESM-005", StringComparison.Ordinal) && entry.Details.Contains("999", StringComparison.Ordinal));
    }
}
