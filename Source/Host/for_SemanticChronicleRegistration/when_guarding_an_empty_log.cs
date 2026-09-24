// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_guarding_an_empty_log : Specification
{
    bool _empty;

    void Because() => _empty = SemanticChronicleRegistration.IsEmpty(ulong.MaxValue);

    [Fact] void should_admit_an_unavailable_tail() => _empty.ShouldBeTrue();
}
