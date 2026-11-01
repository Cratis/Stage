// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_EsmSupport.when_classifying;

public class with_an_audited_pair : a_descriptor
{
    void Because() => _standing = _support.Classify(LanguageVersion.V30, SemanticVersion.V30);

    [Fact] void should_be_audited() => _standing.ShouldEqual(EsmVersionStanding.Audited);
    [Fact] void should_check_exact_audits_before_pair_shape() => new EsmSupport([(new(30, 0), new(30, 1))], new(SemanticVersion.V32, 33)).Classify(new(30, 0), new(30, 1)).ShouldEqual(EsmVersionStanding.Audited);
}
