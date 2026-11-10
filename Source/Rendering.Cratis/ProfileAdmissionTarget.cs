// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents what a Cratis render profile is admitted against.
/// </summary>
/// <param name="Profile">The render profile being admitted.</param>
/// <param name="ApplicationName">The application the profile is rendered for.</param>
/// <param name="Model">The semantic model, or <see langword="null"/> when only the scaffold is planned.</param>
/// <param name="ProfileVersion">The scaffold profile version to admit against, if pinned.</param>
internal sealed record ProfileAdmissionTarget(ArtifactRenderProfile Profile, string ApplicationName, ExecutableSemanticModel? Model, string? ProfileVersion);
