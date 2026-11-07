// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Api;
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

    // Showing a screen by another component's selection is a binding Stage does not translate yet, so it is refused rather than dropped.
    [Fact]
    public void should_refuse_before_returning_a_host_scene()
    {
        const string source = """
            module Work
              screen template Split
                category List
                main
                detail pane
              feature Items
                slice StateView Details
                  readmodel Item
                    id Uuid
                    status String
                  query ItemDetails => Item[]
                  query ItemById => Item optional
                    by id Uuid
                  screen Details
                    template Split
                      main
                        table items
                          column status
                      detail
                        show ItemDetail by id from items.selection
                  screen ItemDetail
                    data Item via query ItemById by id
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
        ((UnsupportedUiSyntax)error).Member.ShouldEqual("ScreenShowSyntax");
        ((UnsupportedUiSyntax)error).Location.Line.ShouldEqual(20);
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
