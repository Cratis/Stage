// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_guarding_the_first_event : Specification
{
    bool _empty;

    void Because() => _empty = SemanticChronicleRegistration.IsEmpty(0);

    [Fact] void should_refuse_a_log_with_sequence_number_zero() => _empty.ShouldBeFalse();
}
