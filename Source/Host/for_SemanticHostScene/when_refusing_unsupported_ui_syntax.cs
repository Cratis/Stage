// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHostScene;

public class when_refusing_unsupported_ui_syntax : Specification
{
    string _directory = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-unsupported-ui-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData("route \"items/details\"")]
    [InlineData("parameter status from data.status")]
    public void should_refuse_before_returning_a_host_scene(string navigation)
    {
        var source = $$"""
            module Work
              feature Items
                slice StateView Details
                  readmodel Item
                    status String
                  query ItemDetails => Item[]
                  screen Details
                    data Item[] via query ItemDetails
                    navigate to Details
                      {{navigation}}
            """;

        // Host loads presentation from source separately; the admitted backend model has no screens.
        var plan = compiled_plan.From("""
            module Work
              feature Items
                slice StateChange Update
                  command Update
                    id Uuid identifier
                    produces event Updated
            """);
        var path = Path.Combine(_directory, "Navigation.play");
        File.WriteAllText(path, source);
        var error = Catch.Exception(() => SemanticHostScene.Load(path, plan.Model));
        error.ShouldBeOfExactType<UnsupportedUiSyntax>();
        error.Message.ShouldContain(UnsupportedUiSyntax.DiagnosticCode);
        ((UnsupportedUiSyntax)error).Member.ShouldEqual("ScreenNavigateSyntax.Route/Parameters");
        ((UnsupportedUiSyntax)error).Location.Line.ShouldEqual(9);
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
