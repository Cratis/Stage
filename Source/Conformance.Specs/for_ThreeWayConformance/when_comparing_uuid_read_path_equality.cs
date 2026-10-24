// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_uuid_read_path_equality(when_comparing_uuid_read_path_equality.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_uuid_read_path_equality.context>
{
    [Fact] void should_compare_both_specifications() => fixture.Outcomes.Length.ShouldEqual(2);
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);
    [Fact] void should_have_no_differences() => fixture.Differences.ShouldBeEmpty();
    [Fact] void should_pass_every_outcome_on_every_path() => fixture.Outcomes.Count(outcome => outcome.Contains("Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal)).ShouldEqual(2);

    public class context : a_three_way_application
    {
        const string Source = """
            command reads protected
            concept MemberId : Uuid
            module Plants
              feature Access
                slice StateChange Watering
                  event AccessGranted
                    memberId MemberId
                  event AccessRevoked
                  event PlantWatered
                  command GrantAccess
                    id String identifier
                    memberId MemberId
                    produces AccessGranted
                      for id
                      memberId = memberId
                  command RevokeAccess
                    id String identifier
                    produces AccessRevoked
                      for id
                  command WaterPlant
                    id String identifier
                    grantId String
                    requestedBy MemberId
                    reads PlantAccessGrant by grantId
                    validate
                      require PlantAccessGrant.memberId == requestedBy
                        message "Only the granted member may water the plant."
                    produces PlantWatered
                      for id
                  specification WateringAsTheGrantedMember
                    given AccessGranted
                      for "grant"
                      memberId = "a1111111-1111-4111-8111-111111111111"
                    when WaterPlant
                      id = "watering"
                      grantId = "grant"
                      requestedBy = "a1111111-1111-4111-8111-111111111111"
                    then PlantWatered
                      for "watering"
                  specification WateringAsAnotherMember
                    given AccessGranted
                      for "grant"
                      memberId = "a1111111-1111-4111-8111-111111111111"
                    when WaterPlant
                      id = "watering"
                      grantId = "grant"
                      requestedBy = "b2222222-2222-4222-8222-222222222222"
                    then error "Only the granted member may water the plant."
                slice StateView Grants
                  readmodel PlantAccessGrant
                    id String key
                    memberId MemberId
                  projection BuildGrant => PlantAccessGrant
                    from AccessGranted
                      id = $eventSourceId
                      memberId = memberId
                    remove with AccessRevoked
                  query GrantById => PlantAccessGrant optional
                    by id String
            """;

        protected override string ApplicationName => "UuidReadPaths";
        protected override string ProjectFile => "UuidReadPaths.csproj";
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("source", Compile())];
        Task Because() => Verify();

        static ExecutableSemanticModel Compile()
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UuidReadPaths"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
            var compilation = new SemanticModelCompiler().Compile("UuidReadPaths", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value.Model;
        }
    }
}
#endif
