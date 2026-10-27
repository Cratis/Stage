// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// A translated Java token with its complete raw-source interval, including Unicode escape introducers.
/// </summary>
/// <param name="Text">The token's translated spelling, with identifier-ignorable characters removed.</param>
/// <param name="Start">The inclusive raw UTF-16 position.</param>
/// <param name="End">The exclusive raw UTF-16 position.</param>
/// <param name="Identifier">Whether the token has Java identifier syntax.</param>
/// <param name="Literal">Whether it is a string, character literal, or text block.</param>
public sealed record JavaSourceToken(string Text, int Start, int End, bool Identifier, bool Literal);
