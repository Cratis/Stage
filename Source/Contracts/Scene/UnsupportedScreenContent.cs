// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay screen mixes a top-level <c>layout</c> directive with
/// other top-level directives. Every current <c>screens.md</c> example has a screen's top-level directives
/// be either exactly one <c>layout</c> reference, or a flat list with no <c>layout</c> reference at all -
/// <see cref="ScreenConverter"/> only supports those two shapes, and fails loudly on a mix rather than
/// silently dropping the directives that don't fit either interpretation.
/// </summary>
/// <param name="screenName">The name of the screen with the unsupported directive mix.</param>
public class UnsupportedScreenContent(string screenName) : Exception($"Screen '{screenName}' mixes a top-level layout directive with other top-level directives, which is not yet supported");
