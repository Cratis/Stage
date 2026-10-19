// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_admitting_command_responses;

public class with_a_composite_response : given.a_composite_command_response
{
    IEnumerable<string> _features = [];

    void Because() => _features = SemanticVersionFeatures.InCommand(CommandIn(Composite)).Select(feature => feature.Message);

    [Fact] void should_admit_a_composite_response() => _features.ShouldBeEmpty();
}
