// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageSceneConversion;

public class when_converting_a_model_without_a_design_system : given.a_model_with_a_design_system_and_free_screens
{
    void Because()
    {
        var start = Source.IndexOf("ui profile Web", StringComparison.Ordinal);
        var withoutDesign = Source[start..];
        _scene = StageSceneConversion.Visit(new Cratis.Screenplay.ScreenplayCompiler().Parse(withoutDesign).Value!);
    }

    [Fact] void should_leave_the_profile_without_a_design_system() => _scene.UiProfiles.Single().DesignSystem.ShouldBeNull();
}
