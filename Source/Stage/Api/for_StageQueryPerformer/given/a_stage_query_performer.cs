// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Api.for_StageQueryPerformer.given;

public class a_stage_query_performer : Specification
{
    protected StageQueryPerformer _performer = null!;

    void Establish() => _performer = new(typeof(DynamicReadModel), "AllReadModels", ["read-models"], byId: false);
}
