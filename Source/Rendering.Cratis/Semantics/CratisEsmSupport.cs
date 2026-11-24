// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Provides the internal source of truth for audited Cratis ESM support and its prospective range.
/// </summary>
internal static class CratisEsmSupport
{
    internal static EsmSupport Descriptor { get; } = new(
        [.. EsmSchemaV30Support.LanguageVersions.Zip(EsmSchemaV30Support.SemanticVersions), (LanguageVersion.V31, SemanticVersion.V31), (LanguageVersion.V32, SemanticVersion.V32), (LanguageVersion.V33, SemanticVersion.V33), (LanguageVersion.V34, SemanticVersion.V34), (LanguageVersion.V36, SemanticVersion.V36), (LanguageVersion.V37, SemanticVersion.V37), (LanguageVersion.V38, SemanticVersion.V38), (LanguageVersion.V40, SemanticVersion.V40), (LanguageVersion.V41, SemanticVersion.V41), (LanguageVersion.V43, SemanticVersion.V43), (LanguageVersion.V44, SemanticVersion.V44), (LanguageVersion.V45, SemanticVersion.V45), (LanguageVersion.V46, SemanticVersion.V46), (LanguageVersion.V47, SemanticVersion.V47), (LanguageVersion.V48, SemanticVersion.V48), (LanguageVersion.V49, SemanticVersion.V49), (LanguageVersion.V53, SemanticVersion.V53), (LanguageVersion.V54, SemanticVersion.V54), (LanguageVersion.V56, SemanticVersion.V56), (LanguageVersion.V57, SemanticVersion.V57)],
        new(SemanticVersion.V32, 33))
    {
        // ESM v57 carries authenticated C# provided-value bodies (Cratis/Stage#1146, Screenplay decision 0167); native bodies remain refused.
        // ESM v56 carries free screens and their adoptions (Cratis/Stage#1150; Screenplay decision 0166). They are a stack-neutral UI contract that the backend target does not render, so the pair is audited and its members scanned: an event-tag expectation that v56 also carries (Cratis/Stage#944) still refuses its specification.
        // ESM v54 carries graph queries that collect nested rows (Cratis/Stage#1144). The sandbox reads them as Chronicle graph views through Arc's graph session, and the renderer refuses them like every graph query (STAGE-ESM-068); its members are scanned so a member Stage has not classified is refused.
        // ESM v53 carries provided values (Cratis/Stage#1116) and audiences and availability gates, which STAGE-ESM-016 refuses per owner until they render; its members are scanned so a model using neither renders as before.
        // ESM v44-v49 are policies that decide from recorded state and row narrowing, which render; they are scanned because v42 screens are ignored and v43 set-once mappings and ordered children render as Chronicle projection behavior (Cratis/Stage#1056). ESM v37 is the command clock and v38 graphs, v40 graph queries, which the sandbox runs and the renderer does not render yet (STAGE-ESM-068). Clock members are refused by the ledger (STAGE-ESM-016), per owning command or specification at run time, except the default UTC time zone and UTC-now command conditions that SemanticCommandClockAdmission admits (Cratis/Stage#1043), so these pairs scan their members.
        MemberScanned = [(LanguageVersion.V37, SemanticVersion.V37), (LanguageVersion.V38, SemanticVersion.V38), (LanguageVersion.V40, SemanticVersion.V40), (LanguageVersion.V41, SemanticVersion.V41), (LanguageVersion.V43, SemanticVersion.V43), (LanguageVersion.V44, SemanticVersion.V44), (LanguageVersion.V45, SemanticVersion.V45), (LanguageVersion.V46, SemanticVersion.V46), (LanguageVersion.V47, SemanticVersion.V47), (LanguageVersion.V48, SemanticVersion.V48), (LanguageVersion.V49, SemanticVersion.V49), (LanguageVersion.V53, SemanticVersion.V53), (LanguageVersion.V54, SemanticVersion.V54), (LanguageVersion.V56, SemanticVersion.V56), (LanguageVersion.V57, SemanticVersion.V57)]
    };
}
