// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_an_older_major_newer_minor : a_descriptor
{
    void Because() => _standing = _support.Classify(new(30, 1), new(30, 1));

    [Fact] void should_not_extend_an_older_audit() => _standing.ShouldEqual(EsmVersionStanding.Unadmitted);
}
