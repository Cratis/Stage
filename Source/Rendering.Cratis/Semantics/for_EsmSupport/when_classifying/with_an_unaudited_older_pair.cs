// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_an_unaudited_older_pair : a_descriptor
{
    void Because() => _standing = _support.Classify(new(31, 0), new(31, 0));

    [Fact] void should_remain_unadmitted() => _standing.ShouldEqual(EsmVersionStanding.Unadmitted);
}
