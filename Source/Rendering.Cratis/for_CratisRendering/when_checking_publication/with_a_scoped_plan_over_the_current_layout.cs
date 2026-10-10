// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_a_scoped_plan_over_the_current_layout : given.a_policy_plan
{
    void Establish() => _existing = _application.Artifacts.ToDictionary(artifact => artifact.RelativePath, Text);
    void Because() => _check = CratisRendering.CheckPublication(_plan, Read);
    [Fact] void should_preserve_the_other_operations_policies() => _check.ShouldBeOfExactType<CratisPublicationCheck.Compatible>();
}
#endif
