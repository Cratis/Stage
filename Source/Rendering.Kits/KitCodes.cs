// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Kits;

/// <summary>Stable rejection codes for target kits.</summary>
public static class KitCodes
{
    /// <summary>A path contains an empty, dot or parent segment.</summary>
    public const string Traversal = "STAGE-KIT-001";

    /// <summary>A path is rooted or drive-qualified.</summary>
    public const string RootedPath = "STAGE-KIT-002";

    /// <summary>A source entry or ancestor is a symbolic link.</summary>
    public const string SymbolicLink = "STAGE-KIT-003";

    /// <summary>Paths collide without regard to case.</summary>
    public const string DuplicatePath = "STAGE-KIT-004";

    /// <summary>A text file is not canonical strict UTF-8.</summary>
    public const string InvalidUtf8 = "STAGE-KIT-005";

    /// <summary>The manifest is missing.</summary>
    public const string MissingManifest = "STAGE-KIT-006";

    /// <summary>The manifest is not structurally valid JSON.</summary>
    public const string InvalidManifest = "STAGE-KIT-007";

    /// <summary>A manifest member is unknown.</summary>
    public const string UnknownMember = "STAGE-KIT-008";

    /// <summary>The target API name, major or version format is unsupported.</summary>
    public const string UnsupportedApi = "STAGE-KIT-009";

    /// <summary>The target API minor is newer than the engine.</summary>
    public const string NewerApi = "STAGE-KIT-010";

    /// <summary>A language pack version is not exact.</summary>
    public const string InvalidLanguagePack = "STAGE-KIT-011";

    /// <summary>An exact language pack is unavailable.</summary>
    public const string MissingLanguagePack = "STAGE-KIT-012";

    /// <summary>A required contract is missing or has a different major.</summary>
    public const string MissingContract = "STAGE-KIT-013";

    /// <summary>A required contract minor is newer than the engine.</summary>
    public const string NewerContract = "STAGE-KIT-014";

    /// <summary>A declared file is missing.</summary>
    public const string MissingFile = "STAGE-KIT-015";

    /// <summary>A file is not declared.</summary>
    public const string UndeclaredFile = "STAGE-KIT-016";

    /// <summary>A manifest declaration is duplicated.</summary>
    public const string DuplicateDeclaration = "STAGE-KIT-017";

    /// <summary>A target identity or diagnostic declaration is invalid.</summary>
    public const string InvalidDeclaration = "STAGE-KIT-018";

    /// <summary>A path is not portable or exceeds the archive limit.</summary>
    public const string NonPortablePath = "STAGE-KIT-019";

    /// <summary>A source exceeds a load limit or contains a special file.</summary>
    public const string LoadLimit = "STAGE-KIT-020";
}
