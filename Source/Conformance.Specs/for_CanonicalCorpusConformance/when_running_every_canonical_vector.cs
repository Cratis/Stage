// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

// The conformance matrix: every vector of the pinned canonical corpus, through partial model loading and then the spec executor, the
// semantic Host admission and the Cratis render planner. See Documentation for how to update conformance-matrix.json when a cell changes on purpose.
//
// The whole matrix runs once in the class fixture, not once per fact: it is 4 surfaces over every vector (planner and admission level, no
// generated build), and the facts below only read the shared result.
public class when_running_every_canonical_vector(when_running_every_canonical_vector.context fixture) : IClassFixture<when_running_every_canonical_vector.context>
{
    [Fact] void should_enumerate_at_least_the_expected_number_of_vectors() => Assert.True(fixture.Actual.Count >= fixture.Expected.MinimumVectorCount, $"{fixture.Actual.Count} vectors enumerated, expected at least {fixture.Expected.MinimumVectorCount}.");
    [Fact] void should_run_the_corpus_version_the_table_was_written_for() => fixture.CorpusVersion.ShouldEqual(fixture.Expected.CorpusVersion);
    [Fact] void should_enumerate_every_golden_esm_version() => Assert.True(fixture.MissingGoldenVersions.Count == 0, $"No vector for golden ESM versions: {string.Join(", ", fixture.MissingGoldenVersions)}.");
    [Fact] void should_account_for_every_corpus_type() => Assert.True(fixture.UnaccountedTypes.Count == 0, $"Corpus types that yield no vector and are not in excludedTypes: {string.Join(", ", fixture.UnaccountedTypes)}.");
    [Fact] void should_have_a_row_for_every_vector() => Assert.True(fixture.VectorsWithoutRows.Count == 0, $"Vectors without a matrix row: {string.Join(", ", fixture.VectorsWithoutRows)}.");
    [Fact] void should_not_keep_rows_for_vectors_that_no_longer_exist() => Assert.True(fixture.RowsWithoutVectors.Count == 0, $"Matrix rows without a vector: {string.Join(", ", fixture.RowsWithoutVectors)}.");
    [Fact] void should_have_no_gap_on_any_surface() => Assert.True(fixture.Gaps.Count == 0, $"{fixture.Gaps.Count} gaps (exception, untyped refusal, partial or silent outcome):{Environment.NewLine}{string.Join(Environment.NewLine, fixture.Gaps)}");
    [Fact] void should_match_the_expected_outcome_of_every_cell() => Assert.True(fixture.Mismatches.Count == 0, $"{fixture.Mismatches.Count} cells differ from conformance-matrix.json:{Environment.NewLine}{string.Join(Environment.NewLine, fixture.Mismatches)}");
    [Fact] void should_load_only_the_per_refusal_schema() => Assert.True(fixture.Expected.SchemaProblems.Count == 0, string.Join(Environment.NewLine, fixture.Expected.SchemaProblems));
    [Fact] void should_record_every_refusal_exactly() => Assert.True(fixture.RecordProblems.Count == 0, string.Join(Environment.NewLine, fixture.RecordProblems));
    [Fact] void should_give_every_refusal_one_disposition() => Assert.True(fixture.DispositionProblems.Count == 0, string.Join(Environment.NewLine, fixture.DispositionProblems));

    public class context
    {
        public context()
        {
            Expected = expected_conformance_table.Load();
            CorpusVersion = canonical_corpus_vectors.CorpusAssembly.GetName().Version!.ToString(3);
            var vectors = canonical_corpus_vectors.All;
            var actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>>();
            foreach (var vector in vectors)
            {
                var cells = new Dictionary<conformance_surface, conformance_observation>();
                foreach (var surface in Enum.GetValues<conformance_surface>())
                {
                    cells[surface] = conformance_outcomes.Execute(surface, vector).GetAwaiter().GetResult();
                }

                actual[vector.Name] = cells;
            }

            Actual = actual;
            Console.WriteLine($"Canonical corpus conformance: {Actual.Count} vectors from Cratis.Screenplay.CanonicalCorpus {CorpusVersion}.");
            foreach (var surface in Enum.GetValues<conformance_surface>())
            {
                var cells = Actual.Values.Select(row => row[surface].Outcome).ToArray();
                Console.WriteLine($"  {surface}: {cells.Count(cell => cell == "supported")} supported, {cells.Count(cell => cell == "refused")} refused, {cells.Count(cell => cell == "expected-refusal")} expected-refusal, {cells.Count(cell => cell.StartsWith("n/a:", StringComparison.Ordinal))} n/a, {cells.Count(cell => cell.StartsWith("gap:", StringComparison.Ordinal))} gap.");
            }

            if (Environment.GetEnvironmentVariable(expected_conformance_table.WriteActualVariable) is { Length: > 0 } path)
            {
                File.WriteAllText(path, expected_conformance_table.Render(Expected, CorpusVersion, Actual));
                File.WriteAllText(Path.ChangeExtension(path, ".details.json"), expected_conformance_table.RenderDetails(Actual));
            }
        }

        public expected_conformance_table Expected { get; }

        public string CorpusVersion { get; }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> Actual { get; }

        public IReadOnlyList<int> MissingGoldenVersions => [.. Cratis.Screenplay.CanonicalCorpus.EsmGoldenVectors.Versions
            .Where(version => !Actual.ContainsKey($"{nameof(Cratis.Screenplay.CanonicalCorpus.EsmGoldenVectors)}.V{version}"))];

        public IReadOnlyList<string> UnaccountedTypes => [.. canonical_corpus_vectors.TypesWithoutVectors.Where(type => !Expected.ExcludedTypes.ContainsKey(type))];

        public IReadOnlyList<string> VectorsWithoutRows => [.. Actual.Keys.Where(name => !Expected.Vectors.ContainsKey(name))];

        public IReadOnlyList<string> RowsWithoutVectors => [.. Expected.Vectors.Keys.Where(name => !Actual.ContainsKey(name))];

        public IReadOnlyList<string> Gaps => [.. Cells().Where(cell => cell.Actual.Outcome.StartsWith("gap:", StringComparison.Ordinal)).Select(cell => $"  {cell.Vector} / {cell.Surface}: {cell.Actual.Outcome}")];

        // A missing surface is never silently skipped.
        public IReadOnlyList<string> Mismatches => [.. Cells()
            .Where(cell => ExpectedFor(cell.Vector, cell.Surface) is null)
            .Select(cell => $"{cell.Vector} / {cell.Surface}: missing expected cell"),
            .. Problems.Where(problem => problem.Contains(": changed-outcome(", StringComparison.Ordinal))];

        public IReadOnlyList<string> RecordProblems => [.. Problems.Where(problem => !IsDisposition(problem) && !problem.Contains(": changed-outcome(", StringComparison.Ordinal))];
        public IReadOnlyList<string> DispositionProblems => [.. Problems.Where(IsDisposition)];

        IReadOnlyList<string> Problems => [.. Cells().SelectMany(cell => ExpectedFor(cell.Vector, cell.Surface) is { } expected
            ? refusal_dispositions.Problems(cell.Vector, cell.Surface, expected, cell.Actual) : [])];

        static bool IsDisposition(string problem) => problem.Contains("disposition(", StringComparison.Ordinal) ||
            problem.Contains(": unauthorized-by-design(", StringComparison.Ordinal) || problem.Contains(": issue-500-only-tracks-", StringComparison.Ordinal);

        IEnumerable<(string Vector, conformance_surface Surface, conformance_observation Actual)> Cells() =>
            Actual.SelectMany(row => row.Value.Select(cell => (Vector: row.Key, Surface: cell.Key, Actual: cell.Value)));

        expected_conformance_cell? ExpectedFor(string vector, conformance_surface surface) =>
            Expected.Vectors.TryGetValue(vector, out var row) && row.TryGetValue(surface, out var cell) ? cell : null;
    }
}
#endif
