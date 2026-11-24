// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Uses the target's public context assembly, not the framework running the renderer.</summary>
internal static class SemanticProvidedContextReferences
{
    static readonly Lazy<PortableExecutableReference> _net10 = new(() => Load("net10.0"));
    static readonly Lazy<PortableExecutableReference> _net11 = new(() => Load("net11.0"));

    internal static PortableExecutableReference ForTargetFramework(string framework) => framework switch
    {
        "net10.0" => _net10.Value,
        "net11.0" => _net11.Value,
        _ => throw new InvalidOperationException($"Unsupported provided-body target framework '{framework}'.")
    };

    static PortableExecutableReference Load(string framework)
    {
        var resource = $"ProvideReferences.{framework}.Cratis.Screenplay.Contexts.dll";
        using var stream = typeof(SemanticProvidedContextReferences).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded provided-context reference '{resource}' is missing.");
        return MetadataReference.CreateFromStream(stream, filePath: resource);
    }
}
