// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Describes an unaudited clock member that the live runtime scopes to one artifact instead of refusing the whole model.
/// </summary>
/// <param name="Member">The unaudited member.</param>
/// <param name="Kind">The kind of artifact it is scoped to: command, specification, readModel or application.</param>
/// <param name="Command">The owning command when the member belongs to one.</param>
internal sealed record SemanticScopedMember(SemanticUnknownMember Member, string Kind, SemanticCommand? Command);
