// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>Observes a structured reference's UTF-16 span in the full emitted file and its intended canonical type.</summary>
/// <param name="Start">Inclusive UTF-16 offset.</param>
/// <param name="End">Exclusive UTF-16 offset.</param>
/// <param name="Intended">Canonical type name, with dotted nested types.</param>
internal sealed record JavaReferenceSite(int Start, int End, string Intended);
