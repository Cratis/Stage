// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_given_read_model : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    SemanticUnsupportedCapability? _refusal;

    void Because()
    {
        var plan = specification_plan.Create(seeded: true);
        _admission = new(plan);
        _refusal = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single());
    }

    [Fact] void should_share_executor_admission() => _refusal.ShouldBeNull();
    [Fact] void should_report_supported() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_not_report_a_refused_capability() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldBeNull();
    [Fact] void should_not_carry_refusal_details() => _admission.Entries.Single(entry => entry.Kind == "specification").Details.ShouldBeNull();
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
}
