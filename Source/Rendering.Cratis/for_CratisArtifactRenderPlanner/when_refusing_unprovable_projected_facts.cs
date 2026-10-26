// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A projected-facts specification only renders when the generated read-model scenario can prove it; what would need a
/// reaction settled, or an instance no given event reaches, still refuses instead of rendering a scenario that proves nothing.
/// </summary>
public class when_refusing_unprovable_projected_facts
{
    const string Source = """
        concept LeadId : Uuid
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
            slice StateView LeadList
              readmodel Leads
                leadId LeadId
                name String
              query AllLeads => Leads[]
              projection LeadsProjection => Leads
                from LeadRegistered
                  leadId = $eventSourceId
                  name = name
              specification <specification>
        """;

    const string Appended = """
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                when append LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0002"
                  name = "Ola"
                then readmodel Leads
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0002"
                  name = "Ola"
        """;

    const string SourcelessAppend = """
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                when append LeadRegistered
                  name = "Ola"
                then readmodel Leads
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
        """;

    const string Unreached = """
                given LeadRegistered
                  for "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0001"
                  name = "Kari"
                then readmodel Leads
                  leadId = "6f1f4a3e-0c0d-4c6a-9a41-3b6a1a0e0002"
                  name = "Ola"
        """;

    const string Reaction = """
              event Welcomed
                name String
              reaction Welcome
                when LeadRegistered
                  produces Welcomed
                    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = name
        """;

    static ArtifactRenderPlan Plan(string specification, string? reactingSlice = null)
    {
        var source = Source.Replace("<specification>", "Proving\n" + specification, StringComparison.Ordinal);
        if (reactingSlice == "LeadList")
        {
            // Only an Automation slice may react, so the read side moves into one.
            source = source.Replace("      specification Proving", Reaction + "\n      specification Proving", StringComparison.Ordinal)
                .Replace("slice StateView LeadList", "slice Automation LeadList", StringComparison.Ordinal);
        }
        else if (reactingSlice is not null)
        {
            source += $"\n    slice Automation {reactingSlice}\n{Reaction}";
        }

        return invoice_model.Plan(invoice_model.Compile(source));
    }

    [Fact]
    void should_admit_an_append_nothing_reacts_to() => Plan(Appended).Success.ShouldBeTrue();

    // A specification runs only its own slice (Screenplay decision 0115): another slice's reaction to the append does not run.
    [Fact]
    void should_admit_an_append_only_another_slice_reacts_to()
    {
        var plan = Plan(Appended, reactingSlice: "Greeting");
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    }

    [Fact]
    void should_refuse_an_append_a_reaction_reacts_to()
    {
        var plan = Plan(Appended, reactingSlice: "LeadList");
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("Reaction append scenarios", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_a_sourceless_append_with_its_own_code()
    {
        var plan = Plan(SourcelessAppend);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-051" && diagnostic.Message.Contains("appended event 'LeadRegistered'", StringComparison.Ordinal));
    }

    [Fact]
    void should_refuse_an_instance_no_given_event_reaches()
    {
        var plan = Plan(Unreached);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("an event it handles on the asserted instance", StringComparison.Ordinal));
    }
}
