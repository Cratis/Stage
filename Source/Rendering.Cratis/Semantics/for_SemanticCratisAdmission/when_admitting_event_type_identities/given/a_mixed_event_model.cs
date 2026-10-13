// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_event_type_identities.given;

public class a_mixed_event_model : Specification
{
    protected ArtifactRenderPlan _plan = null!;

    protected static ArtifactRenderPlan Plan(string storedName)
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("InvoiceIssued", "Renamed", StringComparison.Ordinal)
            .Replace("event Renamed\n", $"event Renamed\n        id \"{storedName}\"\n", StringComparison.Ordinal)
            + "\n      event Recorded_event\n        description String\n";

        return invoice_model.Plan(invoice_model.Compile(source));
    }
}
