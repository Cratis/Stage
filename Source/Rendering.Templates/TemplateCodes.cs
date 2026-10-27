// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Stable safety refusal codes for stage-templates/v1.
/// </summary>
internal static class TemplateCodes
{
    /// <summary>Missing templates/v1 requirement.</summary>
    public const string MissingContract = "STAGE-TPL-001";

    /// <summary>Template exceeds 64 KiB UTF-8.</summary>
    public const string TemplateSize = "STAGE-TPL-002";

    /// <summary>Pre-scan nesting exceeds 32.</summary>
    public const string Nesting = "STAGE-TPL-003";

    /// <summary>Template parse error.</summary>
    public const string Parse = "STAGE-TPL-004";

    /// <summary>Forbidden builtin, global, import, with or alias.</summary>
    public const string Allowlist = "STAGE-TPL-005";

    /// <summary>Undeclared or non-literal partial.</summary>
    public const string Partial = "STAGE-TPL-006";

    /// <summary>Private-use code point in template.</summary>
    public const string TemplatePrivateUse = "STAGE-TPL-007";

    /// <summary>Static helper misuse.</summary>
    public const string Helper = "STAGE-TPL-008";

    /// <summary>Invalid operator table.</summary>
    public const string OperatorTable = "STAGE-TPL-009";

    /// <summary>Unknown section override target.</summary>
    public const string Override = "STAGE-TPL-010";

    /// <summary>CLR access or host-value stringification.</summary>
    public const string HostValue = "STAGE-TPL-020";

    /// <summary>Model or builtin mutation.</summary>
    public const string Mutation = "STAGE-TPL-021";

    /// <summary>Symbol used as text or captured.</summary>
    public const string Symbol = "STAGE-TPL-022";

    /// <summary>Private-use code point in JSON.</summary>
    public const string JsonPrivateUse = "STAGE-TPL-023";

    /// <summary>Malformed helper input or contract.</summary>
    public const string Input = "STAGE-TPL-024";

    /// <summary>Aggregate steps or loop budget.</summary>
    public const string Steps = "STAGE-TPL-030";

    /// <summary>Aggregate UTF-8 output bytes.</summary>
    public const string Output = "STAGE-TPL-031";

    /// <summary>Artifact count.</summary>
    public const string Artifacts = "STAGE-TPL-032";

    /// <summary>Fan-out count.</summary>
    public const string FanOut = "STAGE-TPL-033";

    /// <summary>String length.</summary>
    public const string StringLength = "STAGE-TPL-034";

    /// <summary>Recursion depth.</summary>
    public const string Recursion = "STAGE-TPL-035";

    /// <summary>Plan deadline.</summary>
    public const string Deadline = "STAGE-TPL-036";

    /// <summary>Explicit kit refusal.</summary>
    public const string Refuse = "STAGE-TPL-040";

    /// <summary>Whole-plan refusal already latched.</summary>
    public const string AlreadyRefused = "STAGE-TPL-041";
}
