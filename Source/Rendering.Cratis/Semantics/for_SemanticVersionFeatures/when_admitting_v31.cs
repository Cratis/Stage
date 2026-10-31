// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_admitting_v31 : Specification
{
    [Fact]
    void should_admit_the_anonymous_policy_vector()
    {
        var model = SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(31).Bytes.AsSpan());
        model.Application.Policies.ShouldContain(policy => policy.Condition is SemanticAnonymousCondition);
        SemanticVersionFeatures.Supports(model).ShouldBeTrue();
    }
}
