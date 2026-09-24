// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticHttpBinding.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding.when_querying;

public class after_registering : a_bound_semantic_model
{
    int _keyedStatus;
    string _keyedBody = null!;
    int _allStatus;
    string _allBody = null!;
    int _byIdStatus;
    string _byIdBody = null!;
    int _missingStatus;
    string _missingBody = null!;
    int _protectedStatus;
    int _protectedCompatibilityStatus;

    async Task Because()
    {
        await Request("POST", Route, Payload);
        (_keyedStatus, _keyedBody, _) = await Request("GET", "/api/projects/registration/public-lookup/public-by-id?projectId=3fa85f64-5717-4562-b3fc-2c963f66afa6");
        (_allStatus, _allBody, _) = await Request("GET", "/api/projects/registration/public-lookup/all-public-summaries");
        (_byIdStatus, _byIdBody, _) = await Request("GET", "/api/projects/registration/public-lookup/get-public-summary-by-id?id=3fa85f64-5717-4562-b3fc-2c963f66afa6");
        (_missingStatus, _missingBody, _) = await Request("GET", "/api/projects/registration/public-lookup/public-by-id?projectId=11111111-1111-1111-1111-111111111111");
        (_protectedStatus, _, _) = await Request("GET", "/api/projects/registration/project-lookup/project-by-id?projectId=3fa85f64-5717-4562-b3fc-2c963f66afa6");
        (_protectedCompatibilityStatus, _, _) = await Request("GET", "/api/projects/registration/project-lookup/all-project-summaries");
    }

    [Fact] void should_serve_the_keyed_query() => _keyedStatus.ShouldEqual(200);
    [Fact] void should_return_the_keyed_row() => _keyedBody.ShouldContain("Screenplay");
    [Fact] void should_serve_unrestricted_compatibility() => _allStatus.ShouldEqual(200);
    [Fact] void should_return_the_compatibility_row() => _allBody.ShouldContain("Screenplay");
    [Fact] void should_serve_compatibility_by_id() => _byIdStatus.ShouldEqual(200);
    [Fact] void should_return_the_compatibility_lookup() => _byIdBody.ShouldContain("Screenplay");
    [Fact] void should_return_no_missing_row() => _missingBody.ShouldNotContain("Screenplay");
    [Fact] void should_return_a_successful_missing_lookup() => _missingStatus.ShouldEqual(200);
    [Fact] void should_deny_the_protected_keyed_query() => _protectedStatus.ShouldEqual(403);
    [Fact] void should_not_map_protected_compatibility() => _protectedCompatibilityStatus.ShouldEqual(404);
}
