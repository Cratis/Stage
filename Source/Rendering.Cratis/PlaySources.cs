// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Names Screenplay files and folders under one stable identity and attachment root.
/// </summary>
/// <param name="Root">The Screenplay root. Document identities depend on paths relative to this root.</param>
/// <param name="Paths">Files or folders, absolute or root-relative; empty means all of Root.</param>
/// <param name="CatalogPath">An optional absolute or root-relative authoritative identity catalog.</param>
public sealed record PlaySources(string Root, ImmutableArray<string> Paths, string? CatalogPath = null);
