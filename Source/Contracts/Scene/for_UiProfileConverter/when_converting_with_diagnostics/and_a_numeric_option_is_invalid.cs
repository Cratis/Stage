// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting_with_diagnostics;

public class and_a_numeric_option_is_invalid : Specification
{
    [Theory]
    [InlineData("invalid")]
    [InlineData("NaN")]
    [InlineData("1e9999")]
    public void should_report_the_option_and_leave_other_profiles_convertible(string value)
    {
        var profile = new ScreenplayCompiler().Compile("ui profile Desktop\n  target platform web\n  root template Shell\n    option density 2\n").Value!.UiProfiles!.Single();
        var option = profile.Root!.Options.Single();
        var invalid = profile with { Root = profile.Root with { Options = [option with { Value = value }] } };
        var diagnostics = new ConversionDiagnostics();

        UiProfileConverter.Convert(invalid, diagnostics).ShouldBeEmpty();
        UiProfileConverter.Convert(profile, diagnostics).Single().Root!.Options!["density"]!.GetValue<long>().ShouldEqual(2L);
        diagnostics.All.Single().Code.ShouldEqual("STAGE-SCENE-PROFILE-003");
        diagnostics.All.Single().Construct.ShouldEqual("numeric shell option 'density'");
        diagnostics.All.Single().Path.ShouldEqual("Desktop");
        diagnostics.All.Single().Location.ShouldEqual(option.Location);
    }
}
