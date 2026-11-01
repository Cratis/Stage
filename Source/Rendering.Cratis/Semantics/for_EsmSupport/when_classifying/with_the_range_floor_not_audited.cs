// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_the_range_floor_not_audited : a_descriptor
{
    void Because() => _standing = _support.Classify(LanguageVersion.V32, SemanticVersion.V32);

    [Fact] void should_admit_the_floor() => _standing.ShouldEqual(EsmVersionStanding.NewerWithinRange);
    [Fact] void should_describe_the_typed_range() => _support.Accepts.ToString().ShouldEqual(">=32.0 <33");
    [Fact] void should_respect_a_nonzero_minimum_minor() => new EsmVersionRange(new(32, 2), 33).Contains(new(32, 1)).ShouldBeFalse();
}
