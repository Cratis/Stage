// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class with_an_analysis_exception : given.a_reducer_specification
{
    void Because()
    {
        var previous = PureTransitionAdmission.ReferenceDirectoryOverride.Value;
        try
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Validate();
        }
        finally
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = previous;
        }
    }

    [Fact] void should_preserve_the_analysis_failure()
    {
        var reason = Refusal();
        reason.ShouldContain("Reducer analysis failed: InvalidOperationException:");
        reason.ShouldContain("Reference assemblies");
        reason.ShouldNotContain("cannot supply deterministically");
    }
}
