// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a type a generated file names. A nested type is referenced through its outermost containing type, and
/// the caller appends the nested names and any type arguments to the spelling it gets back.
/// </summary>
/// <param name="Namespace">The namespace of the type, empty for the global namespace.</param>
/// <param name="Name">The C# name of the outermost type, including an <c language="csharp">Attribute</c> suffix when it has one.</param>
/// <param name="Arity">The generic arity of the outermost type.</param>
/// <param name="Context">Where the type is named.</param>
internal sealed record TypeReference(string Namespace, string Name, int Arity, ReferenceContext Context)
{
    /// <summary>
    /// Gets the fully qualified spelling, which always binds to this type whatever else is in scope.
    /// </summary>
    public string Qualified => $"global::{string.Join('.', CatalogNames.Qualify(Namespace, Name).Split('.').Select(Identifiers.EscapeKeyword))}";
}
