// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Defines the stable codes of the declarative planner. Every code is a safety refusal and yields a plan without artifacts.
/// </summary>
public static class DeclarativeCodes
{
    /// <summary>
    /// The renderer or its version is not the declarative planner.
    /// </summary>
    public const string RendererMismatch = "STAGE-DECL-001";

    /// <summary>
    /// The profile inputs are not exactly one input named kit.
    /// </summary>
    public const string InputRoster = "STAGE-DECL-002";

    /// <summary>
    /// The kit bytes do not match the digest the profile recorded.
    /// </summary>
    public const string KitDigestMismatch = "STAGE-DECL-003";

    /// <summary>
    /// The profile target does not match the kit target.
    /// </summary>
    public const string TargetMismatch = "STAGE-DECL-004";

    /// <summary>
    /// The kit language pack is bound to no registered language, or to more than one.
    /// </summary>
    public const string LanguageNotBound = "STAGE-DECL-005";

    /// <summary>
    /// The kit binds more than one language pack, which the planner does not support yet.
    /// </summary>
    public const string LanguagePackCount = "STAGE-DECL-006";

    /// <summary>
    /// The render model could not be built from the request.
    /// </summary>
    public const string RenderModelInvalid = "STAGE-DECL-007";

    /// <summary>
    /// A fan-out item has no string identity.
    /// </summary>
    public const string ItemWithoutId = "STAGE-DECL-008";

    /// <summary>
    /// Generated text contains sentinel-shaped residue, which the structural substitution must never leave behind.
    /// </summary>
    public const string SentinelResidue = "STAGE-DECL-009";

    /// <summary>
    /// An implementation body does not match the content hash its requirement recorded.
    /// </summary>
    public const string BodyIntegrity = "STAGE-DECL-010";

    /// <summary>
    /// A rendered output path is rooted, traverses, or is not portable.
    /// </summary>
    public const string PathInvalid = "STAGE-DECL-011";

    /// <summary>
    /// Two rendered output paths are equal ignoring case.
    /// </summary>
    public const string PathCollision = "STAGE-DECL-012";

    /// <summary>
    /// The assembled text of a file cannot be a text artifact, for example because it starts with a byte order mark.
    /// </summary>
    public const string OutputText = "STAGE-DECL-013";

    /// <summary>
    /// A language pack returned a different number of resolutions than the plan has files.
    /// </summary>
    public const string LanguageContract = "STAGE-DECL-014";
}
