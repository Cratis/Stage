// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity.given;

public class a_gated_reaction : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    protected void WithGate(string gate)
    {
        SetContext(Compile($"policy CanComplete\n  {(gate.StartsWith("```", StringComparison.Ordinal) ? gate : "require " + gate)}\n" + Source
            .Replace("      command CompleteWork", "      command CompleteWork\n        authorize CanComplete", StringComparison.Ordinal)
            .Replace("      reaction CompletePositive", "      reaction CompletePositive\n        runs as system role \"Automation\"", StringComparison.Ordinal)));
    }
}
