// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Represents a single generated source file.
/// </summary>
/// <param name="RelativePath">The file's path, relative to the target application's source root.</param>
/// <param name="Content">The file's full C# source content.</param>
public sealed record RenderedFile(string RelativePath, string Content);
