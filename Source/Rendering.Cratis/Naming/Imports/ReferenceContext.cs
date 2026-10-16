// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents where a type is named, which decides what else a short name could bind to there.
/// </summary>
internal enum ReferenceContext
{
    /// <summary>
    /// A type position, such as a parameter type, a base type or a generic argument, where only types, namespaces,
    /// type parameters and nested types are candidates.
    /// </summary>
    Type = 0,

    /// <summary>
    /// The name of an attribute, which C# also looks up with the <c language="csharp">Attribute</c> suffix.
    /// </summary>
    Attribute = 1,

    /// <summary>
    /// The left side of a member access in an expression, such as <c language="csharp">Guid.Parse</c>, where a member,
    /// parameter or local with the same name hides the type.
    /// </summary>
    Expression = 2,

    /// <summary>
    /// The argument of <c language="csharp">nameof</c>, which binds like an expression.
    /// </summary>
    NameOf = 3
}
