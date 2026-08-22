// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_Slice;

/// <summary>
/// Capability added to this record after it shipped is an <c>init</c> property, which means every caller
/// written against the older shape still compiles and still runs - and lands on whatever the property
/// defaults to.
/// </summary>
/// <remarks>
/// Defaulting to <see langword="null"/> would move the cost of the addition onto every existing consumer,
/// which is the outcome an <c>init</c> property is chosen to avoid: a caller that never heard of the member
/// would hand a slice on to something that reads it, and only fail there.
/// </remarks>
public class when_constructed_without_the_added_members : Specification
{
    Slice _slice = null!;

    void Because() => _slice = new Slice(Guid.NewGuid(), "RegisterInvoice", SliceType.StateChange, [], null, null, []);

    [Fact] void should_carry_no_reactions() => _slice.Reactions.ShouldBeEmpty();
    [Fact] void should_carry_no_screens() => _slice.Screens.ShouldBeEmpty();
    [Fact] void should_carry_no_captures() => _slice.Captures.ShouldBeEmpty();
    [Fact] void should_never_carry_null_reactions() => _slice.Reactions.ShouldNotBeNull();
    [Fact] void should_never_carry_null_screens() => _slice.Screens.ShouldNotBeNull();
    [Fact] void should_never_carry_null_captures() => _slice.Captures.ShouldNotBeNull();
}
