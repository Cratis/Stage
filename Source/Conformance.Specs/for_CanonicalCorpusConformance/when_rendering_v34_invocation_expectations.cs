// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_rendering_v34_invocation_expectations(when_rendering_v34_invocation_expectations.context fixture) : IClassFixture<when_rendering_v34_invocation_expectations.context>
{
    [Fact] void should_load_the_golden() => Assert.Equal("supported", fixture.Actual[conformance_surface.Loader].Outcome);
    [Fact] void should_execute_the_reference_expectations() => Assert.Equal("supported", fixture.Actual[conformance_surface.Executor].Outcome);
    [Fact] void should_render_and_compile_the_exact_debug_sources() => Assert.Equal("supported", fixture.Actual[conformance_surface.Renderer].Outcome);
    [Fact] void should_match_the_reviewed_renderer_cell() => Assert.Equal(fixture.Expected.Vectors["EsmGoldenVectors.V34"][conformance_surface.Renderer].Outcome, fixture.Actual[conformance_surface.Renderer].Outcome);

    public class context
    {
        public expected_conformance_table Expected { get; } = expected_conformance_table.Load();
        public IReadOnlyDictionary<conformance_surface, conformance_observation> Actual { get; }

        public context()
        {
            var vector = canonical_corpus_vectors.All.Single(vector => vector.Name == "EsmGoldenVectors.V34");
            Actual = Enum.GetValues<conformance_surface>().ToDictionary(surface => surface, surface => conformance_outcomes.Execute(surface, vector).GetAwaiter().GetResult());
            if (Environment.GetEnvironmentVariable(expected_conformance_table.WriteActualVariable) is { Length: > 0 } path)
            {
                var actual = new Dictionary<string, IReadOnlyDictionary<conformance_surface, conformance_observation>> { [vector.Name] = Actual };
                File.WriteAllText(path, expected_conformance_table.Render(Expected, canonical_corpus_vectors.CorpusAssembly.GetName().Version!.ToString(3), actual));
                File.WriteAllText(Path.ChangeExtension(path, ".details.json"), expected_conformance_table.RenderDetails(actual));
            }
        }
    }
}
#endif
