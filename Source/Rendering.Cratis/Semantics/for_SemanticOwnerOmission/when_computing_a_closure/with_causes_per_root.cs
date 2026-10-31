// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_causes_per_root : given.an_omission_context
{
    SemanticOwnerOmission _first = null!;
    SemanticOwnerOmission _reversed = null!;
    SemanticId _event;
    SemanticId _query;

    void Establish()
    {
        BuildContext();
        _event = _registerProject.Events.Single().Id;
        _query = _projectLookup.Queries.Single().Id;
    }

    void Because()
    {
        _first = SemanticOwnerOmission.Compute(_context, _index, new Dictionary<SemanticId, string> { [_event] = "STAGE-DEGRADE-001", [_query] = "STAGE-DEGRADE-001" })!;
        _reversed = SemanticOwnerOmission.Compute(_context, _index, new Dictionary<SemanticId, string> { [_query] = "STAGE-DEGRADE-001", [_event] = "STAGE-DEGRADE-001" })!;
    }

    [Fact] void should_report_both_roots_for_the_shared_consuming_slice() => Assert.Equal(new[] { _event, _query }.OrderBy(owner => owner.ToString(), StringComparer.Ordinal), _first.Causes[_projectLookup.Id]);
    [Fact] void should_preserve_both_causes_when_the_command_reads_the_query() => Assert.Equal(new[] { _event, _query }.OrderBy(owner => owner.ToString(), StringComparer.Ordinal), _first.Causes[_registerProject.Id]);
    [Fact] void should_keep_cause_order_independent_of_root_insertion_order() => Assert.Equal(_first.Causes[_projectLookup.Id].ToArray(), _reversed.Causes[_projectLookup.Id].ToArray());
    [Fact] void should_keep_the_ordered_union_independent_of_root_insertion_order() => Assert.Equal(_first.OrderedClosure, _reversed.OrderedClosure);
}
