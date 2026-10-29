// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Target-neutral symbol identity, never prequalified text.
/// </summary>
/// <param name="Namespace">Symbol namespace.</param>
/// <param name="Name">Short name.</param>
public sealed record TemplateSymbol(string Namespace, string Name);
