// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one ordered piece of a resolved file.
/// </summary>
public abstract record DeclarativePiece;

/// <summary>
/// Text produced by the language and the template; the planner normalizes its line endings.
/// </summary>
/// <param name="Text">The generated text.</param>
public sealed record DeclarativeGeneratedPiece(string Text) : DeclarativePiece;

/// <summary>
/// Authored text that the planner keeps byte for byte.
/// </summary>
/// <param name="Text">The authored text.</param>
public sealed record DeclarativeAuthoredPiece(string Text) : DeclarativePiece;
