// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal sealed partial class SemanticApplicationContext
{
    readonly List<InlineCode> _renderedInlineInputs = [];

    internal InlineCode RetainInlineInput(SemanticInlineCodeResult inline)
    {
        if (inline.Refused || inline.Code is null) throw new InvalidArtifactRenderContract("Only verified rendered bodies can contribute inline input provenance.");
        _renderedInlineInputs.Add(inline.Code);
        return inline.Code;
    }

    internal ArtifactRenderPlan WithInlineInputs(ArtifactRenderPlan plan) => _renderedInlineInputs.Count == 0
        ? plan
        : plan.WithInlineCodeFingerprint(CSharpInlineLanguagePack.Fingerprint(_renderedInlineInputs));
}
