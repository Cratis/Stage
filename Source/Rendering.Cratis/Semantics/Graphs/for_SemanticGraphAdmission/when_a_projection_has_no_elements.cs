// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.Graphs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Graphs.for_SemanticGraphAdmission;

public class when_a_projection_has_no_elements : Specification
{
    bool _accepted;
    string _diagnostics = string.Empty;

    void Because()
    {
        var source = graph_projection_sources.Split;
        source = source[..source.IndexOf("        projection PeopleIndex", StringComparison.Ordinal)] + "        projection Empty => graph Organization\n";
        var parsed = new ScreenplayCompiler().Parse(source);
        _accepted = parsed.Success;
        _diagnostics = string.Join(Environment.NewLine, parsed.Diagnostics);
    }

    [Fact] void should_reject_it_before_rendering_instead_of_silently_omitting_its_identity() => _accepted.ShouldBeFalse();
    [Fact] void should_explain_why_no_declaration_can_be_rendered() => _diagnostics.ShouldContain("must contain at least one 'node' or 'edge' block");
}
#endif
