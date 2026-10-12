// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_localized_validation : given.a_profile_source_plan
{
    protected override string ModelSource => base.ModelSource.Replace("        produces ProjectRegistered", "        validate\n          name not empty message $strings.project.name\n        produces ProjectRegistered", StringComparison.Ordinal);

    void Establish() => PlanFull();
    void Because() => _result = CratisRendering.PlanFrom(_loaded, _selection, _options);
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_a_successful_full_baseline() => _full.Success.ShouldBeTrue();
    [Fact] void should_match_every_full_render_artifact_byte() => ShouldMatchFull();
    [Fact] void should_preserve_localized_validation() => CommandText().ShouldContain("global::Projects.GeneratedStrings.Resolve");
}
