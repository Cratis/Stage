// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Cratis.Stage.Conformance.Specs.for_RenderCompilation.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_RenderCompilation;

public class when_selecting_the_phase_a_fixtures : Specification
{
    IReadOnlyDictionary<string, render_capture> _captures = null!;

    void Because() => _captures = render_compilation.Captures;

    [Fact]
    void should_select_exactly_one_vector_for_each_approved_fixture()
    {
        Assert.Equal<string>(["AcmeAdmissionsCorpus.V29", "EventRoutesCorpus.CompositesV8"], _captures.Keys.Order(StringComparer.Ordinal));
        foreach (var capture in _captures.Values)
        {
            capture.AssertAdmitted();
            compilation_layout.Inspect(capture.Plan!).AssertComplete();
        }
    }
}
#endif
