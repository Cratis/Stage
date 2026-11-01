// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRunAdmission;

public class when_checking_source_keyed_captures : Specification
{
    SemanticUnsupportedCapability? _foreign;
    Exception? _local;

    void Because()
    {
        _foreign = Check(local: false);
        _local = Catch.Exception(() => Check(local: true));
    }

    [Fact] void should_run_a_capture_keyed_by_event_source_over_a_foreign_event() => _foreign.ShouldBeNull();
    [Fact] void should_reject_a_model_whose_capture_reads_a_local_event_before_admission() => _local.ShouldBeOfExactType<InvalidSemanticContract>();

    static SemanticUnsupportedCapability? Check(bool local)
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan());
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Kind != SemanticSliceKind.Translate || !local
                        ? slice
                        : slice with { Events = [.. slice.Events.Select(contract => contract with { Origin = null })] })]
                })]
            })]
        };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        return SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single());
    }
}
