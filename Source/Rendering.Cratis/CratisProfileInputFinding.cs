// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// A ui profile input the Cratis target cannot honor, as <see cref="CratisRendering.ResolveProfileInputs"/> reports it.
/// </summary>
/// <param name="Kind">What the Scene engine found.</param>
/// <param name="Code">The stable <c>STAGE-PROFILE-</c> diagnostic code for that kind of finding.</param>
/// <param name="Subject">What the finding is about: the package, layout, theme or template.</param>
/// <param name="Message">The Scene engine's explanation.</param>
public sealed record CratisProfileInputFinding(RenderFindingKind Kind, string Code, string Subject, string Message);
