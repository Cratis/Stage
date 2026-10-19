// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_SpecificationRenderer.given;
using Xunit;
using Xunit.Abstractions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_comparing_production_routes(when_comparing_production_routes.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_production_routes.context>
{
    [Fact] void should_run_a_keyed_override_from_a_literal() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("LiteralOverride: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_run_a_keyed_override_from_a_property() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("PropertyOverride: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_run_an_unkeyed_override_without_a_command_route() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("UnkeyedOverride: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_run_an_override_onto_another_event_source() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("OtherSourceOverride: Reference=Passed, Stage=Passed, Rendered=Passed", StringComparison.Ordinal));
    [Fact] void should_fail_a_different_override_stream_identity_on_every_path() => Assert.Contains(fixture.Outcomes, outcome => outcome.Contains("WrongStream: Reference=Failed, Stage=Failed, Rendered=Failed", StringComparison.Ordinal));
    [Fact] void should_match_all_three_executions() => ThreeWayOutcomes.Report(fixture, output);

    public class context : a_three_way_application
    {
        const string Source = """
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
            eventsource Ledger
              id "stored-ledger"
              identifier String
              stream Entries
                id "stored-entries"
                streamId String
            module Banking
              feature Deposits
                slice StateChange ArchiveLiteral
                  command ArchiveLiteral
                    accountId String identifier
                    period String
                    stream Account.Transactions
                      streamId = period
                    produces Deposited
                      for accountId
                    produces Archived
                      for accountId
                      stream Account.Archive
                        streamId = "archive-1"
                  event Deposited
                  event Archived
                  specification LiteralOverride
                    when ArchiveLiteral
                      accountId = "acc-1"
                      period = "2026-10"
                    then Deposited
                      stream Account.Transactions
                        streamId = "2026-10"
                    then Archived
                      stream Account.Archive
                        streamId = "archive-1"
                slice StateChange ArchiveByProperty
                  command ArchiveByProperty
                    accountId String identifier
                    period String
                    archive String
                    stream Account.Transactions
                      streamId = period
                    produces Filed
                      for accountId
                    produces Stored
                      for accountId
                      stream Account.Archive
                        streamId = archive
                  event Filed
                  event Stored
                  specification PropertyOverride
                    when ArchiveByProperty
                      accountId = "acc-1"
                      period = "2026-10"
                      archive = "2026-archive"
                    then Filed
                      stream Account.Transactions
                        streamId = "2026-10"
                    then Stored
                      stream Account.Archive
                        streamId = "2026-archive"
                  specification WrongStream
                    when ArchiveByProperty
                      accountId = "acc-1"
                      period = "2026-10"
                      archive = "2026-archive"
                    then Filed
                      stream Account.Transactions
                        streamId = "2026-10"
                    then Stored
                      stream Account.Archive
                        streamId = "other-archive"
                slice StateChange AuditUnrouted
                  command AuditUnrouted
                    accountId String identifier
                    produces Audited
                      for accountId
                      stream Account.Audit
                  event Audited
                  specification UnkeyedOverride
                    when AuditUnrouted
                      accountId = "acc-1"
                    then Audited
                      stream Account.Audit
                slice StateChange PostToLedger
                  command PostToLedger
                    accountId String identifier
                    period String
                    stream Account.Transactions
                      streamId = period
                    produces Booked
                      for accountId
                    produces Posted
                      for accountId
                      stream Ledger.Entries
                        streamId = period
                  event Booked
                  event Posted
                  specification OtherSourceOverride
                    when PostToLedger
                      accountId = "acc-1"
                      period = "2026-10"
                    then Booked
                      stream Account.Transactions
                        streamId = "2026-10"
                    then Posted
                      stream Ledger.Entries
                        streamId = "2026-10"
            """;

        protected override string ApplicationName => "Routed";
        protected override string ProjectFile => "Routed.csproj";
        protected override bool ExpectFailedGeneratedTest => true;
        protected override bool RequireStageExecution => true;
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("production-routes", routed_specifications.Compile(Source))];
        Task Because() => Verify();
    }
}
#endif
