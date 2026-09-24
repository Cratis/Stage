// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class with_lowercase_property_names : a_bound_semantic_model
{
    int _status;

    async Task Because() => (_status, _, _) = await Request(
        "POST", Route, "{\"PROJECTID\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"NAME\":\"Screenplay\"}");

    [Fact] void should_bind_without_case_sensitivity() => _status.ShouldEqual(200);
}
