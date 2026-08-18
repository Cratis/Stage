// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

/// <summary>
/// The absence of a construct has to arrive as an empty collection rather than as <see langword="null"/>, so a
/// consumer counting what a slice declares never has to ask whether the model was built by a converter that
/// knew about the member.
/// </summary>
public class when_inspecting_a_slice_declaring_none_of_them : given.a_compiled_model_using_previously_dropped_constructs
{
    [Fact] void should_carry_no_reactions() => _registerInvoice.Reactions.ShouldBeEmpty();
    [Fact] void should_carry_no_screens() => _registerInvoice.Screens.ShouldBeEmpty();
    [Fact] void should_carry_no_captures() => _registerInvoice.Captures.ShouldBeEmpty();
    [Fact] void should_carry_no_null_reactions() => _registerInvoice.Reactions.ShouldNotBeNull();
    [Fact] void should_carry_no_null_screens() => _registerInvoice.Screens.ShouldNotBeNull();
    [Fact] void should_carry_no_null_captures() => _registerInvoice.Captures.ShouldNotBeNull();
    [Fact] void should_leave_the_rest_of_the_slice_alone() => _registerInvoice.Events.Count.ShouldEqual(1);
}
