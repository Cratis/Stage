// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_implicit_generation_at_v16(when_comparing_implicit_generation_at_v16.context fixture) : IClassFixture<when_comparing_implicit_generation_at_v16.context>
{
    [Fact] void should_preserve_implicit_uuid_reuse_on_all_three_paths() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_have_no_three_way_differences() => Assert.Empty(fixture.Differences);

    public class context : a_three_way_application
    {
        protected override string ApplicationName => "ImplicitGeneration";
        protected override string ProjectFile => "ImplicitGeneration.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("v16-reuse", invoice_model.Compile(Source))];
        Task Because() => Verify();

        const string Source = """
            concept Id : Uuid
            module Work
              feature Creation
                slice StateChange Create
                  command Create
                    id Id generated identifier
                    produces Created
                      for id
                    produces Initialized
                      for id
                  event Created
                    id "Original"
                  event Initialized
                  specification ReusingAnUnguardedSource
                    given Created
                      for "11111111-1111-1111-1111-111111111111"
                    when Create
                      for "11111111-1111-1111-1111-111111111111"
                    then Created
                    then Initialized
                  specification NonStrategyUuidFixture
                    when Create
                      for "00112233-4455-6677-8899-aabbccddeeff"
                    then Created
                    then Initialized
            """;
    }
}
#endif
