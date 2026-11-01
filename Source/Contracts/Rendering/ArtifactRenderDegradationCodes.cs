// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>The exact shared degradation vocabulary. These codes do not grant admission or publication permission.</summary>
public static class ArtifactRenderDegradationCodes
{
    /// <summary>An unknown member of a semantic owner.</summary>
    public const string UnknownOwnerMember = "STAGE-DEGRADE-001";

    /// <summary>An unsupported semantic construct.</summary>
    public const string UnsupportedConstruct = "STAGE-DEGRADE-002";

    /// <summary>An inline implementation's language or placement does not match the target.</summary>
    public const string InlineMismatch = "STAGE-DEGRADE-003";

    /// <summary>A newer ESM within an explicitly declared supported range; this code does not establish such a range.</summary>
    public const string NewerEsmWithinDeclaredRange = "STAGE-DEGRADE-004";

    /// <summary>A dependency degradation or omission, including retained cascade placeholders with explicit root causes.</summary>
    public const string DependencyDegradation = "STAGE-DEGRADE-005";
}
