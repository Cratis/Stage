// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_sources_and_mismatched_names : given.a_source_plan
{
    void Establish() => _planOptions = _planOptions with
    {
        Profile = CratisRendering.CreateProfile(_planOptions.ApplicationName, new(_planOptions.ProjectName, "Acme.Projects"))
    };

    async Task Because() => _result = await From();
    [Fact] void should_refuse_the_profile() => _result.Success.ShouldBeFalse();
    [Fact] void should_return_the_typed_profile_diagnostic() => ShouldRefuse("STAGE-CRATIS-001");
    [Fact] void should_emit_no_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
