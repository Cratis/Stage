// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A specification that only projects facts - history, optionally one appended event, then the read model - renders as a
/// read-model scenario, including for a scoped projection, a constant key and running totals, and its protected live
/// collection is queried through the real policy.
/// </summary>
public class when_executing_projected_fact_specifications : a_generated_application
{
    internal const string Source = """
        concept LeadId : Uuid
        concept Amount : Decimal
        type LeadNote
          noteId LeadId
          name String
        policy SalesReps
          require role "SalesRep"
        module Sales
          feature Leads
            slice StateChange RegisterLead
              command RegisterLead
                leadId LeadId identifier
                name String
                produces LeadRegistered
                  for leadId
                  name = name
              event LeadRegistered
                name String
            slice StateChange RenameLead
              command RenameLead
                leadId LeadId identifier
                name String
                produces LeadRenamed
                  for leadId
                  name = name
              event LeadRenamed
                name String
            slice StateChange WinDeal
              command WinDeal
                dealId LeadId identifier
                amount Amount
                produces DealWon
                  for dealId
                  amount = amount
              event DealWon
                amount Amount
            slice StateView LeadList
              readmodel Leads
                leadId LeadId
                name String
              query AllLeads => observable Leads[]
                authorize SalesReps
              projection LeadsProjection => Leads
                from LeadRegistered
                  leadId = $eventSourceId
                  name = name
                from LeadRenamed
                  name = name
              specification ListingTheRegisteredLead
                given caller
                  authenticated
                  role "SalesRep"
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                then readmodel Leads
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
              specification RenamingALead
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                when append LeadRenamed
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari Nordmann"
                then readmodel Leads
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari Nordmann"
              specification QueryingTheLeads
                given caller
                  authenticated
                  role "SalesRep"
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                then query AllLeads
                  result
                    leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                    name = "Kari"
            slice StateChange NoteLead
              command NoteLead
                leadId LeadId identifier
                noteId LeadId
                name String
                produces LeadNoted
                  for leadId
                  noteId = noteId
                  leadId = leadId
                  name = name
              event LeadNoted
                noteId LeadId
                leadId LeadId
                name String
            slice StateView Notes
              readmodel LeadNotes
                leadId LeadId
                title String
                notes LeadNote[]
              query AllLeadNotes => observable LeadNotes[]
                authorize SalesReps
              projection LeadNotesProjection => LeadNotes
                from LeadRegistered
                  leadId = $eventSourceId
                  title = name
                children notes identified by noteId
                  from LeadNoted key noteId
                    parent leadId
                    name = name
              specification ListingTheNotes
                given caller
                  authenticated
                  role "SalesRep"
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                given LeadNoted
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  noteId = "5fa85f64-5717-4562-b3fc-2c963f66afa7"
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Called"
                then query AllLeadNotes
                  result
                    leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                    title = "Kari"
            slice StateView Figures
              readmodel LeadFigures
                summaryId String
                won Amount
                deals Int
              query CurrentFigures => observable LeadFigures[]
                authorize SalesReps
              projection FiguresProjection => LeadFigures
                from DealWon
                  key "global"
                  summaryId = "global"
                  add won by amount
                  increment deals
              specification CountingTheDeals
                given caller
                  authenticated
                  role "SalesRep"
                given DealWon
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0002"
                  amount = 48000
                given DealWon
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0003"
                  amount = 2000
                then readmodel LeadFigures
                  summaryId = "global"
                  won = 50000
                  deals = 2
        """;

    protected override ArtifactRenderPlan CreatePlan() => when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(Source).GetAwaiter().GetResult());

    [Fact]
    async Task should_build_and_pass_the_projected_fact_specifications()
    {
        try
        {
            var build = await Run("projected-facts-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var results = await Run("projected-facts-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", results, StringComparison.Ordinal);
            Assert.Contains("ReadModelScenario", ReadGeneratedFile("Sales/Leads/LeadList/when_listing_the_registered_lead_is_projected.cs"), StringComparison.Ordinal);
            var appended = ReadGeneratedFile("Sales/Leads/LeadList/when_renaming_a_lead_is_projected.cs");
            Assert.Contains("LeadRenamed(", appended, StringComparison.Ordinal);
            Assert.Contains("UsingConstantKey", ReadGeneratedFile("Sales/Leads/Figures/Figures.cs"), StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
