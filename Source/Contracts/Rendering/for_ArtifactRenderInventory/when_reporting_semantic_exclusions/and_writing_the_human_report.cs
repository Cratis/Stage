// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory.when_reporting_semantic_exclusions;

public class and_writing_the_human_report : given.an_excluded_request
{
    void Because() => _text = Text(ArtifactRenderReports.Create(_excluded.WithInventory(ArtifactRenderInventory.Create([])), _context).Unsupported);

    [Fact] void should_name_the_binding_exclusions() => _text.Contains("## Excluded from the executable model", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_escape_the_unit_address() => _text.Contains("Slice: Orders/&lt;Broken&gt; &#91;link&#93; &#96;unit&#96;", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_original_diagnostic_codes_and_locations() => _text.Contains("PLAY0053: Unknown &lt;event&gt; &amp; &#96;name&#96;<br>next (model/orders.play:6:3)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_include_the_exclusion_location() => _text.Contains("model/orders.play:4:2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_dependency_provenance() => _text.Contains("Depends on: Slice: Orders/First", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_reason() => _text.Contains("Needs &lt;Broken&gt; &amp; &#96;access&#96;", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_include_an_actionable_remedy() => _text.Contains("recompile and re-render", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_claim_a_clean_selection() => _text.Contains("No degraded or refused constructs", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_use_only_lf() => _text.Contains('\r').ShouldBeFalse();
}
