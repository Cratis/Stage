// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Collects the references and names emitted by a renderer, then resolves them against the complete file scope.
/// </summary>
/// <param name="request">The file namespace, project and declaration inventory.</param>
internal sealed class CSharpImports(ImportRequest request)
{
    readonly List<TypeReference> _references = [];
    readonly HashSet<string> _types = new(request.ScopeTypeNames, StringComparer.Ordinal);
    readonly HashSet<string> _values = new(request.ScopeValueNames, StringComparer.Ordinal);
    readonly HashSet<string> _pinned = new(request.PinnedNames, StringComparer.Ordinal);
    readonly HashSet<string> _accessed = new(request.MemberAccessNames, StringComparer.Ordinal);
    bool _unknown = request.HasUnknownScope;

    internal string Type(TypeReference reference)
    {
        var token = Token(_references.Count);
        _references.Add(reference);
        return token;
    }

    internal void Scope(IEnumerable<string> types, IEnumerable<string> values)
    {
        _types.UnionWith(types);
        _values.UnionWith(values);
    }

    internal void Pin(IEnumerable<string> names) => _pinned.UnionWith(names);
    internal void Access(IEnumerable<string> names) => _accessed.UnionWith(names);

    internal void Inherits(string fullName)
    {
        var inherited = FrameworkNameCatalog.Current.Inherited(fullName, request.Project);
        if (inherited is null)
        {
            _unknown = true;
            return;
        }

        Scope(inherited.NestedTypes, inherited.Members);
    }

    internal (string Body, ImportResolution Resolution) Resolve(string body, IEnumerable<string> required, string? fileNamespace)
    {
        var remaining = body;
        for (var index = 0; index < _references.Count; index++) remaining = remaining.Replace(Token(index), string.Empty, StringComparison.Ordinal);
        var resolution = ImportResolver.Resolve(request with
        {
            References = _references,
            RequiredImports = required,
            ScopeTypeNames = _types,
            ScopeValueNames = _values,
            PinnedNames = _pinned,
            MemberAccessNames = _accessed,
            HasUnknownScope = _unknown || remaining.Contains('\u0001', StringComparison.Ordinal) || remaining.Contains('\u0002', StringComparison.Ordinal) || fileNamespace != request.FileNamespace
        });
        for (var index = 0; index < _references.Count; index++) body = body.Replace(Token(index), resolution.Spell(_references[index]), StringComparison.Ordinal);
        return (body, resolution);
    }

    static string Token(int index) => $"\u0001{index.ToString(CultureInfo.InvariantCulture)}\u0002";
}
