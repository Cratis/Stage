// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_sourceless_given_event : Specification
{
    SemanticRuntimeAdmission _admission = null!;
    bool _sourceless;

    void Because()
    {
        var plan = compiled_plan.From(specification_plan.Source().Replace(
            "        when RegisterProject",
            "        given ProjectRegistered\n          name = \"Earlier\"\n        when RegisterProject",
            StringComparison.Ordinal));
        var given = plan.Specifications.Values.Single().GivenEvents;
        _sourceless = given.Length == 1 && given[0].EventSource is null;
        _admission = new(plan);
    }

    [Fact] void should_have_one_given_event_without_an_event_source() => _sourceless.ShouldBeTrue();

    [Fact] void should_report_supported() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_not_report_a_refused_capability() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldBeNull();
    [Fact] void should_not_block_the_model() => _admission.Blocking.ShouldBeEmpty();
}
