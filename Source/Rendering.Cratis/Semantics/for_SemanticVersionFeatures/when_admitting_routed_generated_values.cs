// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_routed_generated_values : Specification
{
    internal const string Source = """
        concept Receipt : Uuid
          generate uuidv7
        eventsource Account
          identifier Receipt
          stream Transactions
            streamId String
        module Banking
          feature Recording
            slice StateChange Record
              command Record
                receipt Receipt generated identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces event Recorded
                  for receipt
        """;

    SemanticCommand _command = null!;
    string _code = string.Empty;

    void Establish() => _command = SemanticVersionFeatures.Slices(invoice_model.Compile(Source).Application).Single().Commands.Single();

    void Because()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Source));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        _code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Record.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_admit_a_route_with_a_generated_destination() => SemanticVersionFeatures.InCommand(_command).Except(SemanticVersionFeatures.RoutesInCommand(_command)).ShouldBeEmpty();
    [Fact] void should_check_text_stream_ids_before_building_the_result() => _code.ShouldContain("TryText(Period, out var streamIdPart0)");
    [Fact] void should_wrap_the_atomic_result_with_validation() => _code.ShouldContain("OneOf<EventsWithConcurrencyScopes, ValidationResult>");
    [Fact] void should_admit_routed_responses() => SemanticVersionFeatures.InCommand(_command with { Response = new SemanticScalarCommandResponse(_command.Properties[0].Id, _command.Properties[0].Type) }).Except(SemanticVersionFeatures.RoutesInCommand(_command)).ShouldBeEmpty();
}
