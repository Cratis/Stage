// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_given_read_model : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    string _details = null!;

    void Because()
    {
        var plan = specification_plan.Create(seeded: true);
        _admission = new(plan);
        _details = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())!.Details;
    }

    [Fact] void should_report_unsupported() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("unsupported");
    [Fact] void should_report_the_given_read_model_capability() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldEqual("GivenReadModel");
    [Fact] void should_carry_the_shared_executor_refusal() => _admission.Entries.Single(entry => entry.Kind == "specification").Details.ShouldEqual(_details);
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
}
