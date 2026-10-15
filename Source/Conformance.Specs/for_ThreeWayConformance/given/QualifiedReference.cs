// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Represents one <c language="csharp">global::</c>-qualified type in today's output.
/// </summary>
/// <param name="Type">The type reference offered to the resolver.</param>
/// <param name="Node">The qualified name of the outermost type, from <c language="csharp">global::</c> to its name.</param>
/// <param name="Name">The simple name of the outermost type at the end of <paramref name="Node"/>.</param>
sealed record QualifiedReference(TypeReference Type, SyntaxNode Node, SimpleNameSyntax Name)
{
    /// <summary>
    /// Gets the spans the resolver spells: the qualifier and the type name, but not its type arguments.
    /// </summary>
    public IEnumerable<TextSpan> Spans => [TextSpan.FromBounds(Node.SpanStart, Name.SpanStart), Name.Identifier.Span];
}
#endif
