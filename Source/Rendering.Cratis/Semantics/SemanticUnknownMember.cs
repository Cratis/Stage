// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Identifies a populated member outside the audited surface without retaining authored values.
/// </summary>
/// <param name="Owner">The nearest declaration identity, falling back to the application.</param>
/// <param name="OwnerKind">The structural kind of the owning declaration.</param>
/// <param name="Member">The type and member key, without authored values.</param>
/// <param name="Path">The indexed structural path to this occurrence.</param>
/// <param name="SecurityRelevant">Whether the occurrence must be treated as security-relevant.</param>
internal sealed record SemanticUnknownMember(SemanticId Owner, string OwnerKind, string Member, string Path, bool SecurityRelevant);
