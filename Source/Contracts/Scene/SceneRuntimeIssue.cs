// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Describes an authored Scene construct that can be translated but cannot run faithfully.
/// </summary>
/// <param name="Code">The stable runtime refusal code.</param>
/// <param name="Artifact">The authored action or interaction.</param>
/// <param name="Location">The declaration's source location.</param>
/// <param name="Details">The runtime limitation and its owning issues.</param>
public sealed record SceneRuntimeIssue(string Code, string Artifact, SourceLocation Location, string Details);
