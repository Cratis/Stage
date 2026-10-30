// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_unsupported_shapes : given.an_application_trigger_reaction
{
    void Establish()
    {
        var property = SemanticOccurrenceAdmission.Trigger(_reaction, _context).Properties.Single();
        _trigger = _trigger with { Where = new SemanticComparison(new(property.Id, null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("one"))) };
    }

    void Because() => Inspect();

    [Fact]
    void should_refuse_a_guard()
    {
        var diagnostic = _diagnostics.Single(item => item.Artifact == _reaction.Id && item.Code == "STAGE-ESM-024");
        diagnostic.Message.ShouldContain("'where'");
        diagnostic.Message.ShouldContain("ProcessBatch");
        diagnostic.Message.ShouldContain("BatchRequested");
    }
}
