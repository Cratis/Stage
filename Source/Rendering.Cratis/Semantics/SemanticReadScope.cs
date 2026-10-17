// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves the protected reads of one command for read-backed conditions and mappings.
/// </summary>
/// <param name="command">The command that declares the reads.</param>
/// <param name="context">The indexed semantic application.</param>
/// <param name="locals">The command's shared Handle name allocator, or null when only admission needs property lookups.</param>
internal sealed class SemanticReadScope(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals? locals = null)
{
    /// <summary>
    /// Gets the C# local that holds the named read in the Handle method.
    /// </summary>
    /// <param name="read">The read name.</param>
    /// <returns>The local, escaped when it is a C# keyword.</returns>
    /// <exception cref="InvalidOperationException">The scope has no name allocator.</exception>
    internal string Local(string read)
    {
        var allocated = (locals ?? throw new InvalidOperationException("A read scope without a name allocator cannot render locals.")).Name(Naming.Identifiers.ToCamelCase(read));

        return Naming.Identifiers.EscapeKeyword(allocated);
    }

    /// <summary>
    /// Finds a property of the view a read binds.
    /// </summary>
    /// <param name="read">The read name.</param>
    /// <param name="property">The view property identity.</param>
    /// <returns>The property, or null when the read or property is not declared.</returns>
    internal SemanticProperty? Property(string read, SemanticId property)
    {
        var declared = command.Reads.FirstOrDefault(candidate => candidate.Name == read);

        return declared is not null && context.ReadModels.TryGetValue(declared.ReadModel, out var view)
            ? view.Properties.FirstOrDefault(candidate => candidate.Id == property)
            : null;
    }
}
