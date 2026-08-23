// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Emission;

/// <summary>
/// The advisory marker written after a failed direct-write render operation.
/// </summary>
/// <remarks>
/// The marker does not make the target safe: it does not remove or disable stale artifacts and it is never used
/// as permission to overwrite or delete an existing file. Managed staging and commit belong to ArtifactRenderPlan
/// work in Stage #56 and CLI #101.
/// </remarks>
public static class RenderFailureMarker
{
    /// <summary>
    /// The deterministic marker path relative to the render target.
    /// </summary>
    public const string RelativePath = ".stage-render-failed";

    /// <summary>
    /// The deterministic marker content.
    /// </summary>
    public const string Content =
        "Stage rendering failed. This target is unsafe and incomplete.\n" +
        "Files from earlier runs may remain, including artifacts blocked by the failed run.\n" +
        "This marker is advisory only: it does not disable, overwrite, or delete any artifact.\n" +
        "Render into a fresh target and review the failure before building, running, or deploying this output.\n" +
        "Managed ArtifactRenderPlan staging and commit are tracked by Stage #56 and CLI #101.\n";
}
