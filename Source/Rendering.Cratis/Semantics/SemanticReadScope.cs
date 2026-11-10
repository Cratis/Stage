// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Resolves the reads of one command, or of one policy that decides from recorded state, for read-backed conditions and mappings.
/// </summary>
internal sealed class SemanticReadScope
{
    readonly Func<string, SemanticReadModel?> _readModel;
    readonly Func<string, string> _local;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticReadScope"/> class for the protected reads of a command.
    /// </summary>
    /// <param name="command">The command that declares the reads.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="locals">The command's shared Handle name allocator, or null when only admission needs property lookups.</param>
    internal SemanticReadScope(SemanticCommand command, SemanticApplicationContext context, SemanticHandleLocals? locals = null)
    {
        _readModel = read => command.Reads.FirstOrDefault(candidate => candidate.Name == read) is { } declared && context.ReadModels.TryGetValue(declared.ReadModel, out var view) ? view : null;
        _local = read => (locals ?? throw new InvalidOperationException("A read scope without a name allocator cannot render locals.")).Name(read);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticReadScope"/> class for the reads of a policy that decides from recorded state.
    /// </summary>
    /// <param name="policy">The policy that declares the reads.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="local">Names the C# local that holds a read of the policy.</param>
    internal SemanticReadScope(SemanticPolicy policy, SemanticApplicationContext context, Func<string, string> local)
    {
        _readModel = read => policy.Reads.FirstOrDefault(candidate => candidate.Name == read) is { } declared && context.ReadModels.TryGetValue(declared.ReadModel, out var view) ? view : null;
        _local = local;
    }

    /// <summary>
    /// Gets the C# local that holds the named read in the Handle method.
    /// </summary>
    /// <param name="read">The read name.</param>
    /// <returns>The local, escaped when it is a C# keyword.</returns>
    /// <exception cref="InvalidOperationException">The scope has no name allocator.</exception>
    internal string Local(string read)
    {
        return Naming.Identifiers.EscapeKeyword(_local(Naming.Identifiers.ToCamelCase(read)));
    }

    /// <summary>
    /// Finds a property of the view a read binds.
    /// </summary>
    /// <param name="read">The read name.</param>
    /// <param name="property">The view property identity.</param>
    /// <returns>The property, or null when the read or property is not declared.</returns>
    internal SemanticProperty? Property(string read, SemanticId property) =>
        _readModel(read)?.Properties.FirstOrDefault(candidate => candidate.Id == property);
}
