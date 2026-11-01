// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_a_mixed_minor_pair : a_descriptor
{
    void Because() => _standing = _support.Classify(new(32, 1), new(32, 0));

    [Fact] void should_be_malformed() => _standing.ShouldEqual(EsmVersionStanding.Malformed);
}
