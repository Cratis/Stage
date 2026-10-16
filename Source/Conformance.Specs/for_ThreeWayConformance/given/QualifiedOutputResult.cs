// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

/// <summary>
/// Represents the outcome of rewriting qualified output through the import resolver.
/// </summary>
/// <param name="Failures">Every binding, compiler error or inventory entry that differs; empty when the proof holds.</param>
/// <param name="ShortSpellings">How many references the resolver spelled by their short name.</param>
/// <param name="Rewritten">The rewritten files.</param>
sealed record QualifiedOutputResult(IReadOnlyList<string> Failures, int ShortSpellings, IReadOnlyList<RenderedFile> Rewritten);
#endif
