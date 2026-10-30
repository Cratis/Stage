// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Names why <see cref="CSharpImports"/> could not prove which names are in scope in a generated file, which makes it
/// keep every type fully qualified.
/// </summary>
internal enum UnknownScopeReason
{
    /// <summary>The scope is known, so types are spelled by their short names.</summary>
    None,

    /// <summary>The renderer declared the scope unknown, for example because authored text was moved or a base type is not cataloged.</summary>
    Declared,

    /// <summary>A base class is neither declared in the file nor cataloged, so its members are not known.</summary>
    UnknownBase,

    /// <summary>The file declares a block namespace or several namespaces instead of one file-scoped namespace.</summary>
    NotFileScopedNamespace,

    /// <summary>The file already carries using directives of its own.</summary>
    OwnUsings,

    /// <summary>The file has syntax errors.</summary>
    ParseError,

    /// <summary>The file contains a conditional compilation directive.</summary>
    ConditionalDirective
}
