// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay screen directive is a subtype of
/// <c>ScreenDirectiveSyntax</c> that <see cref="ScreenDirectiveConverter"/> does not yet know how to
/// convert (or, for a <c>layout</c>/<c>slot</c> directive, is not the sole top-level directive
/// <see cref="ScreenConverter"/> expects - see its own remarks).
/// </summary>
/// <param name="typeName">The name of the unrecognized <c>ScreenDirectiveSyntax</c> subtype.</param>
public class UnknownScreenDirective(string typeName) : Exception($"'{typeName}' is not a known screen directive");
