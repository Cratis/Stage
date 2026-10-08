// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_an_admitted_specification : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(specification_plan.Create());

    [Fact] void should_report_supported() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_have_no_refusal() => _admission.Entries.Single(entry => entry.Kind == "specification").Details.ShouldBeNull();
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
}
