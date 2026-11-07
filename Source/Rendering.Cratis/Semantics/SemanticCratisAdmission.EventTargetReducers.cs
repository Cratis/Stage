// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static ArtifactRenderDiagnostic? EventTargetReducerDiagnostic(SemanticReducer reducer, SemanticSlice slice, SemanticApplicationContext context)
    {
        if (reducer.Target != SemanticProjectionTargetKind.Event || IsExclusivePublicReducer(reducer, slice, context)) return null;
        return Error(SemanticVersionFeatures.PublicEvents, $"Reducer '{reducer.Name}' needs one exclusive, untagged public event in its outbound translation, distinct local private transitions.", reducer.ReadModel);
    }

    static bool IsExclusivePublicReducer(SemanticReducer reducer, SemanticSlice slice, SemanticApplicationContext context) =>
        SemanticTranslationSupport.Reducer(reducer, slice, context.Events) &&
        !context.Projections.Values.Any(projection => projection.ReadModel == reducer.ReadModel) &&
        context.Reducers.Count(candidate => candidate.ReadModel == reducer.ReadModel) == 1;
}
