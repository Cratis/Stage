// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory.when_reporting_semantic_exclusions.given;

public class an_excluded_request : for_ArtifactRenderInventory.given.an_inventory
{
    protected ArtifactRenderPlan _excluded = null!;
    protected string _text = string.Empty;

    void Establish()
    {
        var exclusion = new SemanticExclusion(
            SemanticExclusionKind.Slice,
            "Orders/<Broken> [link] `unit`",
            null,
            new(4, 2, "model/orders.play"),
            [Diagnostic.Error("PLAY0053", "Unknown <event> & `name`\r\nnext", new(6, 3, "model/orders.play"))])
        {
            Cause = SemanticExclusionCause.Dependent,
            Reason = "Needs <Broken> & `access`",
            DependsOn = [new(SemanticExclusionKind.Slice, "Orders/First")]
        };
        _request = _request with { Exclusions = [exclusion] };
        _excluded = ArtifactRenderPlan.Create(_request, [], []);
    }

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
