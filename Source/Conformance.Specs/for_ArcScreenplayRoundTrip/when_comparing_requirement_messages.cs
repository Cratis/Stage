// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip;

public class when_comparing_requirement_messages : Specification
{
    const string AuthoredMessage = "A positive amount is required.";
    const string Source = """
        concept AccountId : Uuid
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                amount Decimal
                validate
                  require amount > 0
                produces event Deposited
                  amount Decimal = amount
        """;

    ExecutableSemanticModel _omitted;
    ExecutableSemanticModel _default;
    ExecutableSemanticModel _authored;
    SemanticModelComparison _equivalent;
    SemanticModelComparison _distinct;

    void Establish()
    {
        _omitted = given.round_trip_fixtures.Compile(Source);
        _default = WithMessage(SemanticRequirement.DefaultMessage);
        _authored = WithMessage(AuthoredMessage);
    }

    void Because()
    {
        _equivalent = SemanticModelEquivalence.Compare(given.requirement_message_comparison.Project(_omitted), given.requirement_message_comparison.Project(_default));
        _distinct = SemanticModelEquivalence.Compare(given.requirement_message_comparison.Project(_omitted), given.requirement_message_comparison.Project(_authored));
    }

    [Fact] void should_compare_an_omitted_message_equal_to_the_effective_default() => _equivalent.Differences.ShouldBeEmpty();
    [Fact] void should_keep_a_distinct_authored_message_as_a_difference() => Assert.Single(_distinct.Differences).Path.EndsWith("/requirements[0]/message", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_general_comparer_strict() => SemanticModelEquivalence.Compare(_omitted, _default).Differences.ShouldNotBeEmpty();
    [Fact] void should_leave_the_raw_omitted_message_unchanged() => Requirement(_omitted).Message.ShouldBeNull();
    [Fact] void should_preserve_the_authored_message_in_the_projection() => Requirement(given.requirement_message_comparison.Project(_authored)).Message.ShouldEqual(AuthoredMessage);

    static ExecutableSemanticModel WithMessage(string message) => given.round_trip_fixtures.Compile(
        Source.Replace("require amount > 0", $"require amount > 0\n            message \"{message}\"", StringComparison.Ordinal));

    static SemanticRequirement Requirement(ExecutableSemanticModel model) =>
        model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Requirements.Single();
}
#endif
