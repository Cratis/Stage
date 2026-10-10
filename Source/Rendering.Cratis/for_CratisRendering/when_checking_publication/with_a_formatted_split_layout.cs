// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_formatted_split_layout : given.a_policy_plan
{
    void Establish() => _existing = _application.Artifacts.ToDictionary(artifact => artifact.RelativePath, artifact => "// Destination formatting differs.\n" + Text(artifact));
    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_recognize_the_selection_independent_layout_by_declarations() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
}
#endif
