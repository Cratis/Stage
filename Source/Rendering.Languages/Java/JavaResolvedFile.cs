// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>Pairs the unchanged file resolution with observations of its structured references.</summary>
/// <param name="Resolution">The ordinary file resolution.</param>
/// <param name="Sites">The observed references, empty for refused or non-Java files.</param>
internal sealed record JavaResolvedFile(DeclarativeFileResolution Resolution, ImmutableArray<JavaReferenceSite> Sites);
