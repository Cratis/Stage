// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Represents a destination file that a publisher must delete atomically with the plan's artifact writes, or refuse publication.
/// </summary>
/// <param name="RelativePath">The portable application-root-relative path of the file to remove.</param>
/// <param name="ExpectedSha256">The lowercase SHA-256 of the file's LF-normalized UTF-8 bytes; any other content is drift and must be refused.</param>
/// <param name="Model">The semantic identity whose planned artifact replaces the removed file.</param>
public sealed record ArtifactRemoval(string RelativePath, string ExpectedSha256, SemanticId Model);
