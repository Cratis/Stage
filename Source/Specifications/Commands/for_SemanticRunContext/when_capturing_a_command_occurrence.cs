// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Types;
using NSubstitute;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRunContext;

public class when_capturing_a_command_occurrence : Specification
{
    const string AuthoredInstant = "2027-03-04T12:00:00.123+02:00";
    const string ChildInstant = "2027-03-05T10:00:00.4560000Z";
    const string Source = """
        concept InvoiceId : String
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                invoiceId InvoiceId identifier
                produces event InvoiceIssued
                  issuedAt DateTime = $context.occurred
              specification AtTheAuthoredInstant
                given clock "2027-03-04T12:00:00.123+02:00"
                when IssueInvoice
                  invoiceId = "invoice-one"
                then InvoiceIssued
                  issuedAt = "2027-03-04T10:00:00.123Z"
        """;

    readonly AdvancingClock _clock = new();
    readonly List<DateTimeOffset?> _appendOccurrences = [];
    SemanticRunContext _context = null!;
    SemanticRunContext _other = null!;
    SemanticRunContext _parent = null!;
    SemanticRunContext _child = null!;
    DateTimeOffset _first;
    DateTimeOffset _repeated;
    DateTimeOffset _independent;
    DateTimeOffset _authored;
    DateTimeOffset _invoked;
    string _mapped = null!;
    int _initialReads;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockApp"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("clock"), "clock", "clock.play", Source);
        var compilation = new SemanticModelCompiler().Compile("ClockApp", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        var command = plan.Commands.Values.Single();
        var specification = plan.Specifications.Values.Single();
        var types = new SemanticRuntimeTypes(plan);
        var commandType = types.ForCommand(command);
        var eventLog = Substitute.For<IEventLog>();
        eventLog.Append(new EventSourceId("any"), new object(), occurred: null).ReturnsForAnyArgs(call =>
        {
            _appendOccurrences.Add(call.Arg<DateTimeOffset?>());
            return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
        });
        var fallbackOptions = new SemanticSpecificationRunOptions { Clock = _clock };
        _context = new(commandType, command, specification with { GivenClock = null }, fallbackOptions, types, eventLog, plan);
        _other = new(commandType, command, specification with { GivenClock = null }, fallbackOptions, types, eventLog, plan);
        var authoredOptions = new SemanticSpecificationRunOptions { Clock = new UnexpectedClock() };
        _parent = new(commandType, command, specification, authoredOptions, types, eventLog, plan);
        _child = new(commandType, command, specification with { GivenClock = ChildInstant }, authoredOptions, types, eventLog, plan, _parent);
        _initialReads = _clock.Reads;
    }

    async Task Because()
    {
        _first = _context.Occurred;
        _repeated = _context.Occurred;
        _independent = _other.Occurred;
        _authored = _parent.Occurred;
        _invoked = _child.Occurred;
        var (fact, destination) = _context.Produce(_context.Command.Produces.Single());
        _mapped = ((SemanticTextValue)fact.Values.Single().Value).Value;
        await _context.Append(fact, destination, CancellationToken.None);
    }

    [Fact] void should_capture_the_fallback_lazily() => _initialReads.ShouldEqual(0);
    [Fact] void should_capture_the_first_provider_instant() => _first.ShouldEqual(DateTimeOffset.UnixEpoch);
    [Fact] void should_reuse_the_occurrence() => _repeated.ShouldEqual(_first);
    [Fact] void should_capture_separate_contexts_independently() => _independent.ShouldEqual(_first.AddSeconds(1));
    [Fact] void should_read_the_provider_once_per_fallback_context() => _clock.Reads.ShouldEqual(2);
    [Fact] void should_prefer_the_authored_occurrence_without_reading_the_provider() => _authored.ShouldEqual(DateTimeOffset.Parse(AuthoredInstant, CultureInfo.InvariantCulture));
    [Fact] void should_prefer_the_invoked_commands_own_authored_occurrence() => _invoked.ShouldEqual(DateTimeOffset.Parse(ChildInstant, CultureInfo.InvariantCulture));
    [Fact] void should_map_the_same_occurrence_as_the_append_time() => DateTimeOffset.Parse(_mapped, CultureInfo.InvariantCulture).ShouldEqual(_appendOccurrences.Single()!.Value);
    [Fact] void should_append_at_the_captured_occurrence() => _appendOccurrences.Single().ShouldEqual(_first);
    [Fact] void should_keep_authored_absence_distinct_from_the_fallback() => _context.AuthoredOccurrence.ShouldBeNull();
    [Fact] void should_keep_fixture_absence_distinct_from_the_fallback() => SemanticRunContext.OccurrenceOf(_context.Specification).ShouldBeNull();
    [Fact] void should_record_the_authored_absence() => _context.Occurrences.Single().ShouldBeNull();

    sealed class AdvancingClock : TimeProvider
    {
        internal int Reads { get; private set; }
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddSeconds(Reads++);
    }

    sealed class UnexpectedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("An authored occurrence must not read the fallback clock.");
    }
}
