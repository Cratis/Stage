// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_rendering_production_routes : Specification
{
    const string Header = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Transactions
            id "stored-transactions"
            streamId String
          stream Archive
            id "stored-archive"
            streamId String
          stream Audit
            id "stored-audit"
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId String identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces event Deposited
                  for accountId
        """;

    string _code = string.Empty;
    IReadOnlyList<string> _unkeyedCodes = [];

    void Because()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(Header + """

                produces event Archived
                  for accountId
                  stream Account.Archive
                    streamId = period
        """));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        _code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Deposit.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        var unkeyed = invoice_model.Plan(invoice_model.Compile(Header + """

                produces event Audited
                  for accountId
                  stream Account.Audit
        """));
        _unkeyedCodes = [.. unkeyed.Diagnostics.Select(diagnostic => diagnostic.Code)];
    }

    [Fact] void should_render_the_override_source() => _code.ShouldContain("EventSource = typeof(");
    [Fact] void should_render_the_override_stream() => _code.ShouldContain("EventStream = ");
    [Fact] void should_render_the_override_stream_id_from_its_own_local() => _code.ShouldContain("EventStreamId = production1StreamIdPart0");
    [Fact] void should_check_the_override_stream_id() => _code.ShouldContain("TryText(Period, out var production1StreamIdPart0)");
    [Fact] void should_check_the_override_stream_id_before_the_command_route() => _code.IndexOf("out var production1StreamIdPart0", StringComparison.Ordinal).ShouldBeLessThan(_code.IndexOf("out var streamIdPart0", StringComparison.Ordinal));
    [Fact] void should_refuse_an_unkeyed_override_under_a_keyed_command_route() => _unkeyedCodes.ShouldContain("STAGE-ESM-036");
}
