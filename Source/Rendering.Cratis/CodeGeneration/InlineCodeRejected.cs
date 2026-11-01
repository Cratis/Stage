// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// The exception that is thrown when authored inline code cannot be placed safely in generated source.
/// </summary>
public sealed class InlineCodeRejected : Exception
{
    internal InlineCodeRejected(ImmutableArray<InlineCodeDiagnostic> diagnostics)
        : base(string.Join(Environment.NewLine, diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")))
    {
        Diagnostics = diagnostics;
    }

    internal ImmutableArray<InlineCodeDiagnostic> Diagnostics { get; }
}
