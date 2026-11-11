// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// A value in an <c lang="text">http</c> block.
/// </summary>
/// <param name="Kind">Where the value comes from.</param>
/// <param name="Text">The literal text, the name of the call property, or the dotted path.</param>
/// <param name="Literal">The typed value of a literal; null for anything else.</param>
public sealed record HttpValue(HttpValueKind Kind, string Text, JsonNode? Literal = null);
