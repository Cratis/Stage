// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_projection_admission_aligned_to_observed_events : Specification
{
    [Theory]
    [InlineData(false, "supported")]
    [InlineData(true, "unsupported")]
    public void should_only_admit_projections_requested_by_the_specification(bool reachesUnsupported, string status)
    {
        var source = $$"""
            module Work
              feature Work
                slice StateChange Save
                  event Changed
                  event Unrelated
                  command Save
                    id String identifier
                    produces Changed
                      for id
                  specification Saving
                    when Save
                      id = "one"
                    then Changed
                      for "one"
                    then readmodel Current
                      id = "one"
                slice StateView Current
                  readmodel Current
                    id String key
                  projection CurrentProjection => Current
                    no automap
                    from Changed
                      id = $eventSourceId
                slice StateView Other
                  readmodel OtherView
                    id String key
                    count Int
                  projection UnadmittableProjection => OtherView
                    no automap
                    from {{(reachesUnsupported ? "Changed" : "Unrelated")}}
                      id = $eventSourceId
                      add count by 3
            """;
        var admission = new SemanticRuntimeAdmission(compiled_plan.From(source));
        admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual(status);
    }
}
