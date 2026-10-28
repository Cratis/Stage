// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Reports invalid Java type-facts JSON or an invalid canonical type or member name.
/// </summary>
/// <param name="message">The reason the facts cannot be admitted.</param>
public sealed class JavaTypeFactsException(string message) : Exception(message);
