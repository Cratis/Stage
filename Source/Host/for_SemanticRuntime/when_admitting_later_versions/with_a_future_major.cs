// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_future_major : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(a_plan_at_an_esm_pair.From(33, 1));

    [Fact] void should_refuse_the_whole_model() => _admission.Blocking.Single().Kind.ShouldEqual("application");
    [Fact] void should_report_the_stage_version_gate() => _admission.Blocking.Single().Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_range() => _admission.Blocking.Single().Details!.Contains(">=32.0 <33", StringComparison.Ordinal).ShouldBeTrue();
}
