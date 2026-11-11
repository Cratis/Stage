// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Invocations.for_InvocationIdentity;

public class when_deriving_the_identity_of_an_invocation : Specification
{
    static readonly InvocationOrigin _origin = new(InvocationOriginKind.ReactionBranch, "Fulfillment/PostToWarehouse/branch 2", "event 41");

    string _first = null!;
    string _again = null!;
    string _otherContractMajor = null!;
    string _otherCausation = null!;
    string _otherAddress = null!;
    string _otherAbility = null!;

    void Because()
    {
        _first = InvocationIdentity.For(_origin, new("Slack", "Post", 1));
        _again = InvocationIdentity.For(_origin with { }, new("Slack", "Post", 1));
        _otherContractMajor = InvocationIdentity.For(_origin, new("Slack", "Post", 2));
        _otherCausation = InvocationIdentity.For(_origin with { Causation = "event 42" }, new("Slack", "Post", 1));
        _otherAddress = InvocationIdentity.For(_origin with { Address = "Fulfillment/PostToWarehouse/branch 3" }, new("Slack", "Post", 1));
        _otherAbility = InvocationIdentity.For(_origin, new("Slack", "Reply", 1));
    }

    [Fact] void should_be_the_same_for_the_same_origin_and_causation() => _again.ShouldEqual(_first);
    [Fact] void should_not_change_with_the_contract_major() => _otherContractMajor.ShouldEqual(_first);
    [Fact] void should_differ_for_another_causation() => _otherCausation.ShouldNotEqual(_first);
    [Fact] void should_differ_for_another_address() => _otherAddress.ShouldNotEqual(_first);
    [Fact] void should_differ_for_another_ability() => _otherAbility.ShouldNotEqual(_first);
    [Fact] void should_carry_the_prefix_and_a_fixed_length_digest() => (_first.StartsWith(InvocationIdentity.Prefix, StringComparison.Ordinal) && _first.Length == InvocationIdentity.Prefix.Length + 32).ShouldBeTrue();
}
