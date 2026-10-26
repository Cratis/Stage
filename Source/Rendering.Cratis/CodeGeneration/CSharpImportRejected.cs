// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>Refuses generated source whose imports cannot preserve its bindings.</summary>
/// <param name="reason">The unproved scope or binding that prevents emission.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Only the internal import resolver throws this refusal; it is not a public API.")]
internal sealed class CSharpImportRejected(string reason) : Exception(reason)
{
    internal const string Code = "STAGE-CSHARP-IMPORT";
}
