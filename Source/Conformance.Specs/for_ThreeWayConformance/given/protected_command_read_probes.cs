// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

// These V25 probes exercise Arc's invocation-scoped guard, not replay of a prepared reference decision.
// ProtectedCommandReadRaceVectors invocation coverage and exclusions are recorded by #557's race-vector specs.
internal static class protected_command_read_probes
{
    internal const string Accepted = """
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Specifications;
        using ProtectedReads.Storage.Decisions.Use;
        using Xunit;

        namespace ProtectedReads.Probes;

        public class when_accepting_a_decision_without_an_outcome : Specification, IDisposable
        {
            readonly CommandScenario<RequireApproval> _scenario = new();
            CommandResult _result = null!;

            void Establish() => _scenario.Given.ForEventSource("reference").Events(new Stored("present"));

            async Task Because() => _result = await _scenario.Execute(new RequireApproval(Approved: true, Id: "target", SourceId: "reference"));

            [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
            [Fact] void should_append_nothing() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(0);

            public void Dispose() => _scenario.Dispose();
        }
        """;

    internal const string Conflicting = """
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Validation;
        using Cratis.Specifications;
        using ProtectedReads.Storage.Decisions.Use;
        using Xunit;

        namespace ProtectedReads.Probes;

        public class when_a_relevant_event_is_appended_concurrently : Specification, IDisposable
        {
            readonly CommandScenario<Consume> _scenario = new();
            CommandResult _result = null!;

            void Establish()
            {
                _scenario.Given.ForEventSource("reference").Events(new Stored("original"));
                _scenario.AppendConcurrently("reference", new Stored("changed"));
            }

            async Task Because() => _result = await _scenario.Execute(new Consume(SourceId: "reference", Id: "target"));

            [Fact] void should_report_a_concurrency_violation() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
            [Fact] void should_append_no_command_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(0);

            public void Dispose() => _scenario.Dispose();
        }
        """;

    internal const string Unrelated = """
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Specifications;
        using ProtectedReads.Storage.Decisions.Use;
        using Xunit;

        namespace ProtectedReads.Probes;

        public class when_an_irrelevant_event_is_appended_concurrently : Specification, IDisposable
        {
            readonly CommandScenario<Consume> _scenario = new();
            CommandResult _result = null!;

            void Establish()
            {
                _scenario.Given.ForEventSource("reference").Events(new Stored("original"));
                _scenario.AppendConcurrently("reference", new Requested("elsewhere", "reference"));
            }

            async Task Because() => _result = await _scenario.Execute(new Consume(SourceId: "reference", Id: "target"));

            [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
            [Fact] void should_append_the_consumed_event() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(1);

            public void Dispose() => _scenario.Dispose();
        }
        """;

    internal static IEnumerable<(string Name, string Source)> Files =>
    [
        ("ProtectedReadAccepted.cs", Accepted),
        ("ProtectedReadConflict.cs", Conflicting),
        ("ProtectedReadUnrelated.cs", Unrelated)
    ];
}
#endif
