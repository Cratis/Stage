// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_refused_read_keys : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    readonly Dictionary<SemanticId, int> _factCounts = [];

    void Establish() => _plan = refused_read_keys.Compile();

    async Task Because()
    {
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => _factCounts.Add(id, count) };
        var selected = _plan.Specifications.Values.Where(specification => refused_read_keys.Names.Contains(specification.Name, StringComparer.Ordinal) || specification.Name == "ReadPathMappingAcrossSources");
        _report = await executor.Run(_plan, new([.. selected.Select(specification => specification.Id)]), new());
    }

    [Fact] void should_execute_all_six_refusals_and_the_valid_key_control() => _report.Results.Count.ShouldEqual(7);
    [Fact] void should_match_every_authored_expectation() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    [Fact] void should_reject_empty_keys_with_the_missing_view_message() => Result("RefusedEmptyKey").Trace!.Rejection.ShouldEqual("Current '' does not exist.");
    [Fact] void should_preserve_whitespace_in_the_missing_view_message() => Result("RefusedWhitespaceKey").Trace!.Rejection.ShouldEqual("Current '   ' does not exist.");
    [Fact] void should_reject_the_exact_star_key() => Result("RefusedStarKey").Trace!.Rejection.ShouldEqual("Current '*' does not exist.");
    [Fact] void should_reject_a_hash_anywhere_in_the_key() => Result("RefusedHashKey").Trace!.Rejection.ShouldEqual("Current 'a#b' does not exist.");
    [Fact] void should_reject_padded_keys_without_trimming_them() => Result("RefusedPaddedKey").Trace!.Rejection.ShouldEqual("Current ' padded ' does not exist.");
    [Fact] void should_reject_a_star_key_even_with_a_stored_instance() => Result("RefusedStarKeyWithStoredInstance").ExecutionKind.ShouldEqual("Rejected");
    [Fact] void should_keep_the_stored_star_fact_without_consuming_it() => _factCounts[SemanticId.Parse(Result("RefusedStarKeyWithStoredInstance").SpecificationId)].ShouldEqual(1);
    [Fact] void should_append_nothing_for_any_refused_key() => _report.Results.Where(result => refused_read_keys.Names.Contains(result.Name, StringComparer.Ordinal)).ShouldEachConformTo(result => result.Trace!.Facts.Count == 0);
    [Fact] void should_still_map_a_valid_read_across_sources() => Result("ReadPathMappingAcrossSources").Trace!.Facts.Count.ShouldEqual(1);

    SemanticSpecificationRunRecord Result(string name) => _report.Results.Single(result => result.Name == name);
}
