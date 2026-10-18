// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConverter;

public class when_creating_a_profile_with_conversion_diagnostics : Specification
{
    Exception? _error;
    Exception? _warning;

    void Because()
    {
        _error = Catch.Exception(() => CratisRendering.CreateProfile("Items", new("Items", "Items"), Scene(DiagnosticSeverity.Error)));
        _warning = Catch.Exception(() => CratisRendering.CreateProfile("Items", new("Items", "Items"), Scene(DiagnosticSeverity.Warning)));
    }

    [Fact] void should_refuse_conversion_errors() => _error.ShouldBeOfExactType<InvalidArtifactRenderContract>();
    [Fact] void should_name_the_conversion_error() => _error!.Message.ShouldContain("STAGE-SCENE-ARRANGEMENT-002: Unsupported container");
    [Fact] void should_not_refuse_warnings() => _warning.ShouldBeNull();

    static SceneApplication Scene(DiagnosticSeverity severity) => new([], [], [], [], [], [])
    {
        ConversionDiagnostics = [new("STAGE-SCENE-ARRANGEMENT-002", severity, "container", "Shell", SourceLocation.Start, "Unsupported container")]
    };
}
