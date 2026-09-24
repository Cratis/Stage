// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_posting;

public class with_a_duplicate_name : a_bound_semantic_model
{
    int _status;
    string _body = null!;

    async Task Because()
    {
        await Request("POST", Route, Payload);
        (_status, _body, _) = await Request("POST", Route, Payload.Replace("3fa85f64-5717-4562-b3fc-2c963f66afa6", "ae84c798-199c-4b86-b50d-1bb0fabfab12", StringComparison.Ordinal));
    }

    [Fact] void should_reject_the_constraint() => _status.ShouldEqual(400);
    [Fact] void should_name_the_constraint() => _body.ShouldContain("UniqueProjectName");
}
