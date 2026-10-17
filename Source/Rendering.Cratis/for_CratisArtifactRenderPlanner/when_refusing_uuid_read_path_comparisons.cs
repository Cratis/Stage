// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_uuid_read_path_comparisons : Specification
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

    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(Source));

    [Fact] void should_refuse_with_STAGE_ESM_044() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-044");
    [Fact] void should_not_refuse_the_version() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
