// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_a_newer_minor_within_range : Specification
{
    SemanticRuntimeAdmission _admission = null!;

    void Because() => _admission = new(a_plan_at_an_esm_pair.From(32, 1));

    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_report_one_degradation_for_the_application() => _admission.Entries.Count(entry => entry.Kind == "application").ShouldEqual(1);
    [Fact] void should_say_the_pair_is_unaudited() => _admission.Entries.Single(entry => entry.Kind == "application").Details!.StartsWith("STAGE-DEGRADE-004:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_entry_non_blocking() => _admission.Entries.Single(entry => entry.Kind == "application").Status.ShouldEqual("supported");
}
