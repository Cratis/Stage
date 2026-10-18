// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_uuid_read_path_comparisons : Specification
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

    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunRecord _accepted = null!;
    SemanticSpecificationRunRecord _rejected = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UuidReadPaths"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("UuidReadPaths", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
        _accepted = Result("WateringAsTheGrantedMember");
        _rejected = Result("WateringAsAnotherMember");
    }

    [Fact] void should_accept_the_granted_member() => _accepted.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_reject_another_member() => _rejected.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_match_the_reference_runner() => _plan.Specifications.Values.ShouldEachConformTo(specification =>
    {
        var reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        var result = _report.Results.Single(result => result.SpecificationId == specification.Id.ToString());

        return reference.Passed && result.ExecutionKind == reference.Execution.Kind.ToString() &&
            result.Trace!.Rejection == (reference.Execution as SemanticRejected)?.Details;
    });

    SemanticSpecificationRunRecord Result(string name) =>
        _report.Results.Single(result => result.SpecificationId == _plan.Specifications.Values.Single(specification => specification.Name == name).Id.ToString());
}
