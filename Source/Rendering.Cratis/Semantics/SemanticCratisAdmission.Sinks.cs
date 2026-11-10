// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    /// <summary>
    /// The retired code for a query over a read model stored in a named sink.
    /// </summary>
    /// <remarks>
    /// Codes are permanent, so the constant stays as the record of what the code meant. Stage refused such a query while the pinned
    /// Arc release read every rendered query from the application's own MongoDB database. Arc 22.184.0 reads a read model marked
    /// <c language="csharp">[Sink("name")]</c> from that sink's server, so the query renders and this refusal is no longer emitted from the Stage release
    /// that pins it. Note that <see cref="SemanticVersionFeatures.SecretReadModelProperty"/> also uses the number for an unrelated warning.
    /// </remarks>
    internal const string QueryOverNamedSink = "STAGE-ESM-069";
}
