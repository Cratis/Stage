// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

// The conformance matrix: every vector of the pinned canonical corpus, through the strict model loader and then the spec executor, the
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
    [Fact] void should_not_keep_unnecessary_by_design_entries() => Assert.True(fixture.UnnecessaryByDesign.Count == 0, $"Unnecessary by-design entries (the code no longer appears in the actual outcome):{Environment.NewLine}{string.Join(Environment.NewLine, fixture.UnnecessaryByDesign)}");
    [Fact] void should_track_every_refusal() => Assert.True(fixture.UntrackedRefusals.Count == 0, $"Refused cells without a tracking issue number:{Environment.NewLine}{string.Join(Environment.NewLine, fixture.UntrackedRefusals)}");

    public class context
    {
        public context()
        {
            Expected = expected_conformance_table.Load();
            CorpusVersion = canonical_corpus_vectors.CorpusAssembly.GetName().Version!.ToString(3);
            var vectors = canonical_corpus_vectors.All;
            var actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, string>>();
            foreach (var vector in vectors)
            {
                var cells = new Dictionary<conformance_surface, string>();
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
                var cells = Actual.Values.Select(row => row[surface]).ToArray();
                Console.WriteLine($"  {surface}: {cells.Count(cell => cell == "supported")} supported, {cells.Count(cell => cell.StartsWith("refused:", StringComparison.Ordinal))} refused, {cells.Count(cell => cell.StartsWith("expected-refusal:", StringComparison.Ordinal))} expected-refusal, {cells.Count(cell => cell.StartsWith("n/a:", StringComparison.Ordinal))} n/a, {cells.Count(cell => cell.StartsWith("gap:", StringComparison.Ordinal))} gap.");
            }

            if (Environment.GetEnvironmentVariable(expected_conformance_table.WriteActualVariable) is { Length: > 0 } path)
            {
                File.WriteAllText(path, expected_conformance_table.Render(Expected, CorpusVersion, Actual));
            }
        }

        public expected_conformance_table Expected { get; }

        public string CorpusVersion { get; }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<conformance_surface, string>> Actual { get; }

        public IReadOnlyList<int> MissingGoldenVersions => [.. Cratis.Screenplay.CanonicalCorpus.EsmGoldenVectors.Versions
            .Where(version => !Actual.ContainsKey($"{nameof(Cratis.Screenplay.CanonicalCorpus.EsmGoldenVectors)}.V{version}"))];

        public IReadOnlyList<string> UnaccountedTypes => [.. canonical_corpus_vectors.TypesWithoutVectors.Where(type => !Expected.ExcludedTypes.ContainsKey(type))];

        public IReadOnlyList<string> VectorsWithoutRows => [.. Actual.Keys.Where(name => !Expected.Vectors.ContainsKey(name))];

        public IReadOnlyList<string> RowsWithoutVectors => [.. Expected.Vectors.Keys.Where(name => !Actual.ContainsKey(name))];

        public IReadOnlyList<string> Gaps => [.. Cells().Where(cell => cell.Actual.StartsWith("gap:", StringComparison.Ordinal)).Select(cell => $"  {cell.Vector} / {cell.Surface}: {cell.Actual}")];

        // A vector without a row is reported by VectorsWithoutRows; a row that lacks a surface key is a mismatch, never a skipped cell.
        public IReadOnlyList<string> Mismatches => [.. Cells()
            .Where(cell => !cell.Actual.StartsWith("gap:", StringComparison.Ordinal) && Expected.Vectors.ContainsKey(cell.Vector))
            .Select(cell => (cell.Vector, cell.Surface, cell.Actual, Expected: ExpectedFor(cell.Vector, cell.Surface)))
            .Where(cell => cell.Expected is null || !(cell.Surface == conformance_surface.Renderer
                ? renderer_refusals.Matches(cell.Expected.Outcome, cell.Actual)
                : cell.Expected.Outcome == cell.Actual))
            .Select(cell => $"  {cell.Vector} / {cell.Surface}: expected {cell.Expected?.Outcome ?? "<no cell in the row>"}, actual {cell.Actual}")];

        public IReadOnlyList<string> UntrackedRefusals => [
            .. Expected.Vectors.SelectMany(vector => vector.Value.Select(cell => (Vector: vector.Key, Surface: cell.Key, Cell: cell.Value)))
                .Where(cell => cell.Surface != conformance_surface.Renderer && cell.Cell.Outcome.StartsWith("refused:", StringComparison.Ordinal) && cell.Cell.Issue is null)
                .Select(cell => $"  {cell.Vector} / {cell.Surface}: {cell.Cell.Outcome}"),
            .. RendererCoverageProblems
        ];

        // Coverage checks actual fingerprints, not just the expected code set. A same-code refusal on a new artifact is untracked.
        IReadOnlyList<string> RendererCoverageProblems => [.. Actual.SelectMany(vector =>
            ExpectedFor(vector.Key, conformance_surface.Renderer) is { } expected
                ? renderer_refusals.CoverageProblems(vector.Key, expected, vector.Value[conformance_surface.Renderer])
                    .Select(problem => $"  {vector.Key} / Renderer: {problem}")
                : [])];

        public IReadOnlyList<string> UnnecessaryByDesign => [.. RendererCoverageProblems
            .Where(problem => problem.Contains("unnecessary by-design entry", StringComparison.Ordinal))];

        IEnumerable<(string Vector, conformance_surface Surface, string Actual)> Cells() =>
            Actual.SelectMany(row => row.Value.Select(cell => (Vector: row.Key, Surface: cell.Key, Actual: cell.Value)));

        expected_conformance_cell? ExpectedFor(string vector, conformance_surface surface) =>
            Expected.Vectors.TryGetValue(vector, out var row) && row.TryGetValue(surface, out var cell) ? cell : null;
    }
}
#endif
