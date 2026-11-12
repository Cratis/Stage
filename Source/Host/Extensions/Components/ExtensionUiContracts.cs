// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Ui;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Reads the UI contract of a model, which is where Screenplay says which screen places which extension component and which extensions
/// a screen needs.
/// </summary>
public static class ExtensionUiContracts
{
    /// <summary>
    /// Compiles the model's screens against the extension contracts the lock pins.
    /// </summary>
    /// <param name="modelPath">A <c language="shell">.play</c> file or a folder of them.</param>
    /// <param name="model">The <see cref="ExecutableSemanticModel"/> of the model.</param>
    /// <returns>The <see cref="UiContract"/>, or <see langword="null"/> when the model does not compile.</returns>
    public static UiContract? Read(string modelPath, ExecutableSemanticModel model)
    {
        var compiler = new PlayFileCompiler();
        var result = Directory.Exists(modelPath) ? compiler.CompileFolder(modelPath).Result : compiler.CompileFile(modelPath).Result;
        if (!result.Success) return null;

        var files = Directory.Exists(modelPath) ? Directory.EnumerateFiles(modelPath, "*.play", SearchOption.AllDirectories) : [modelPath];
        var extensions = ExtensionContractResolver.Current.Resolve(result.Value!, files);
        return UiContractBuilder.For(result.Value!, model, model.Application.Name, extensions);
    }
}
