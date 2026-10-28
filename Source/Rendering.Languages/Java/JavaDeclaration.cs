// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Registers a declaration once, even when constructor handles repeat in its owning file.
/// </summary>
/// <param name="Symbol">The original canonical symbol, with a dotted nested name.</param>
/// <param name="Allocated">The allocated symbol, including any renamed ancestor prefixes.</param>
/// <param name="Path">The declaring file's relative path.</param>
public sealed record JavaDeclaration(TemplateSymbol Symbol, TemplateSymbol Allocated, string Path);
