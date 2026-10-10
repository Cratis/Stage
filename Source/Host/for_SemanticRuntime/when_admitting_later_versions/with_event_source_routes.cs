// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_event_source_routes : Specification
{
    internal const string Source = """
        concept AccountId : String
        concept Month : Int
        eventsource Account
          identifier AccountId
          stream Transactions
            streamId Month
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                month Month
                amount Decimal
                stream Account.Transactions
                  streamId = month
                produces event Deposited
                  for accountId
                  amount Decimal = amount
              specification Depositing
                when Deposit
                  accountId = "acc-1"
                  month = 1
                  amount = 10
                then Deposited
                  for "acc-1"
                  stream Account.Transactions
                    streamId = 1
                  amount = 10
        """;

    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From(Source);

    void Because() => _admission = new(_plan);

    [Fact] void should_compile_an_esm_v8_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_admit_the_event_source_and_routed_command() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_support_the_routed_command() => _admission.Entries.Single(entry => entry.Kind == "command").Status.ShouldEqual("supported");
    [Fact] void should_support_the_routed_specification() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_admit_routed_specification_execution() => SemanticRunAdmission.Check(_plan, _plan.Specifications.Values.Single()).ShouldBeNull();

    [Theory]
    [InlineData("given")]
    [InlineData("then")]
    [InlineData("append")]
    [InlineData("unrouted")]
    public void should_admit_every_occurrence_routing_form(string form)
    {
        var specification = _plan.Specifications.Values.Single();
        var occurrence = specification.ThenEvents.Single();
        var baseline = specification with { ThenEvents = [] };
        var routed = form switch
        {
            "given" => baseline with { GivenEvents = [occurrence] },
            "append" => baseline with { When = null, WhenAppended = new(occurrence.EventContract, occurrence.Values) { Route = occurrence.Route } },
            "unrouted" => baseline with { ThenEvents = [occurrence with { Route = null, Unrouted = true }] },
            _ => specification
        };
        SemanticRunAdmission.SpecificationFeatures(routed).ShouldBeEmpty();
    }
}
