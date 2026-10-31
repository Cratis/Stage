// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_reachable_translate_projection : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        const string source = """
            module Work
              feature Work
                slice StateChange Work
                  event Saved
                  command Save
                    id String identifier
                    produces Saved
                      for id
                  specification Saving
                    when Save
                      id = "one"
                    then Saved
                      for "one"
                slice Translate LegacySync
                  readmodel Snapshot
                    id String key
                  projection Build => Snapshot
                    from Saved
                      id = $eventSourceId
            """;
        _plan = compiled_plan.From(source);
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_specification() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("unsupported");
    [Fact] void should_preserve_the_translate_slice_diagnostic() => _admission.Entries.Single(entry => entry.Kind == "specification").Details!.ShouldContain("STAGE-ESM-024: Slice 'LegacySync'");
}
