// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The default caller a rendered specification runs as when the model gives it none and the exercised artifact has no
/// modeled authorization.
/// </summary>
/// <remarks>
/// Such an artifact renders <c language="csharp">[Authorize]</c>, so an Arc scenario without a principal would deny it. The
/// specification instead supplies an authenticated principal with no roles and no claims, labelled in the generated
/// source so a reader can see it is a rendering default rather than modeled behavior.
/// </remarks>
internal static class SemanticDefaultCaller
{
    /// <summary>
    /// Gets the comment labelling the default caller in generated source.
    /// </summary>
    public const string Comment =
        "// Authenticated test caller (rendering default): the model has no authorize clause for this artifact, which Stage renders as [Authorize].";
}
