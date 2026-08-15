// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a Screenplay layout <c>template</c> container has a
/// <c>TemplateContainerKind</c> that <see cref="LayoutConverter"/> does not yet know how to convert.
/// </summary>
/// <param name="kind">The unrecognized <see cref="TemplateContainerKind"/>.</param>
public class UnknownTemplateContainerKind(TemplateContainerKind kind) : Exception($"'{kind}' is not a known template container kind");
