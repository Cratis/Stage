// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHostScene;

public class when_refusing_guarded_interactions : Specification
{
    string _directory = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-guarded-interactions-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [Theory]
    [InlineData("click", false)]
    [InlineData("double click", false)]
    [InlineData("select", true)]
    public void should_refuse_before_returning_a_host_scene(string trigger, bool otherwise)
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
                    table Item
                      column status
                      on {{trigger}}
                        when item.status == "open"
                          notify info "Open"
            """;
        if (otherwise) source += "\n            otherwise\n              notify info \"Closed\"";

        // Host loads presentation from source separately; the admitted backend model has no screens.
        var plan = compiled_plan.From("""
            module Work
              feature Items
                slice StateChange Update
                  command Update
                    id Uuid identifier
                    produces event Updated
            """);
        var path = Path.Combine(_directory, "Guarded.play");
        File.WriteAllText(path, source);
        var error = Catch.Exception(() => SemanticHostScene.Load(path, plan.Model));
        error.ShouldBeOfExactType<UnsupportedGuardedInteraction>();
        error.Message.ShouldContain(UnsupportedGuardedInteraction.DiagnosticCode);
        error.Message.ShouldContain("https://github.com/Cratis/Stage/issues/209");
        ((UnsupportedGuardedInteraction)error).Location.Line.ShouldEqual(11);
    }

    void Destroy() => Directory.Delete(_directory, recursive: true);
}
