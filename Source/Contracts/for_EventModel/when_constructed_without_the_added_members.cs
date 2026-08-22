// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModel;

/// <summary>
/// Capability added to this record after it shipped is an <c>init</c> property, which means every caller
/// written against the older shape still compiles and still runs - and lands on whatever the property
/// defaults to.
/// </summary>
/// <remarks>
/// Defaulting to <see langword="null"/> would move the cost of the addition onto every existing consumer,
/// which is the outcome an <c>init</c> property is chosen to avoid: a caller that never heard of the member
/// would hand a model on to something that reads it, and only fail there.
/// </remarks>
public class when_constructed_without_the_added_members : Specification
{
    EventModel _model = null!;

    void Because() => _model = new EventModel(Guid.NewGuid(), "Invoicing", []);

    [Fact] void should_carry_no_concepts() => _model.Concepts.ShouldBeEmpty();
    [Fact] void should_carry_no_policies() => _model.Policies.ShouldBeEmpty();
    [Fact] void should_never_carry_null_concepts() => _model.Concepts.ShouldNotBeNull();
    [Fact] void should_never_carry_null_policies() => _model.Policies.ShouldNotBeNull();
}
