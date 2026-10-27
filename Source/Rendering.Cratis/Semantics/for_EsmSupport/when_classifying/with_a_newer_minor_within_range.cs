// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_a_newer_minor_within_range : a_descriptor
{
    EsmVersionStanding _later;

    void Because()
    {
        _standing = _support.Classify(new(32, 1), new(32, 1));
        _later = _support.Classify(new(32, 7), new(32, 7));
    }

    [Fact] void should_admit_the_first_minor() => _standing.ShouldEqual(EsmVersionStanding.NewerWithinRange);
    [Fact] void should_admit_a_later_minor() => _later.ShouldEqual(EsmVersionStanding.NewerWithinRange);
}
