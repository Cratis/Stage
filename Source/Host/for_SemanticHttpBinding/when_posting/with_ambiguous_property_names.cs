// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class with_ambiguous_property_names : a_bound_semantic_model
{
    int _status;
    string _body = null!;

    async Task Because() => (_status, _body, _) = await Request(
        "POST", Route, "{\"projectId\":\"3fa85f64-5717-4562-b3fc-2c963f66afa6\",\"name\":\"Screenplay\",\"NAME\":\"Other\"}");

    [Fact] void should_reject_ambiguous_keys() => _status.ShouldEqual(400);
    [Fact] void should_identify_the_ambiguity() => _body.ShouldContain("Ambiguous command property");
}
