// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_admitting_the_v2_golden_vector : Specification
{
    [Fact] void should_name_each_owned_refusal_without_partial_output() => given.golden_vector_refusals.AssertNamedRefusals(2);
}
