// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_unused_rule_concept : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(GenerationFixturesCorpus.V21.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with
            {
                Commands = [.. slice.Commands.Where(command => command.Name != "TenantValue")],
                Specifications = [.. slice.Specifications.Where(specification => specification.Name != "GeneratorBodyNotExecuted")]
            }] }] }]
        });
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_keep_the_rule_concept_in_the_model() => _plan.Model.Application.Concepts.ShouldContain(concept => concept.Generation!.Kind == "implementation");
    [Fact] void should_admit_the_live_application() => _admission.Blocking.ShouldBeEmpty();
}
