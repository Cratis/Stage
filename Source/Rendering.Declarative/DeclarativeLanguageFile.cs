// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one mapped file of a plan, as handed to a language pack together with every other file.
/// </summary>
/// <param name="Path">The validated relative path of the file.</param>
/// <param name="Segments">The ordered segments of the file.</param>
public sealed record DeclarativeLanguageFile(string Path, ImmutableArray<DeclarativeSegment> Segments);
