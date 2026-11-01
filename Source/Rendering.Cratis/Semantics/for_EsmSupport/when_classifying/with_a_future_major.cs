// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_a_future_major : a_descriptor
{
    EsmVersionStanding _later;

    void Because()
    {
        _standing = _support.Classify(new(33, 0), new(33, 0));
        _later = _support.Classify(new(35, 2), new(35, 2));
    }

    [Fact] void should_refuse_the_exclusive_major() => _standing.ShouldEqual(EsmVersionStanding.FutureMajor);
    [Fact] void should_refuse_later_majors() => _later.ShouldEqual(EsmVersionStanding.FutureMajor);
}
