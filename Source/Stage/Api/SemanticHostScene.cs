// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Api;

internal static class SemanticHostScene
{
    internal static SceneApplication Load(string modelPath, ExecutableSemanticModel model)
    {
        // ESM does not carry authored screens or layouts. Translate only that presentation from syntax;
        // commands and read models for the default scene come from the already-admitted semantic model.
        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileFile(modelPath).Result;
        if (!result.Success)
        {
            throw new InvalidSemanticModel([.. result.Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => $"{diagnostic.Location.Path}({diagnostic.Location.Line},{diagnostic.Location.Column}): {diagnostic.Message}")]);
        }

        var syntax = result.Value!;
        var scene = SceneSynthesizer.Synthesize(new ScreenplaySceneVisitor().Visit(syntax), model);

        // The converters run without a collector here, so report the members that are admitted but not translated yet (a form field's picker).
        var admitted = new ConversionDiagnostics();
        new UiSyntaxAdmission(admitted).VisitApplication(syntax);

        return scene with { ConversionDiagnostics = [.. scene.ConversionDiagnostics, .. admitted.All] };
    }
}
