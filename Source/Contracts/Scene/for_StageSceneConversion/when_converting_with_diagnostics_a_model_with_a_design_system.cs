// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageSceneConversion;

public class when_converting_with_diagnostics_a_model_with_a_design_system : given.a_model_with_a_design_system_and_free_screens
{
    void Because() => _scene = StageSceneConversion.Convert(_application);

    [Fact] void should_carry_the_design_system_on_the_profile() => _scene.UiProfiles.Single().DesignSystem!.Name.ShouldEqual("ShopDesign");
    [Fact] void should_report_nothing_when_the_design_system_is_complete() => _scene.ConversionDiagnostics.ShouldBeEmpty();
}
