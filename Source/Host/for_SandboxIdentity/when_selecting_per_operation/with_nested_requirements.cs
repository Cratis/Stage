// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation;

public class with_nested_requirements : given.a_started_multimodule_host
{
    async Task Because() => await Send("GET", "/api/sales/restricted/nested/restricted-lookup/restricted-items");

    [Fact] void should_use_the_expected_persona() => _persona.ShouldEqual("persona:Merchandiser");
    [Fact] void should_return_the_expected_status() => _status.ShouldEqual(StatusCodes.Status200OK);

    [Fact]
    void should_include_module_feature_and_operation_requirements()
    {
        var query = SemanticModelWalker.Slices(_model).SelectMany(located => located.Slice.Queries).Single(query => query.Name == "RestrictedItems");
        string.Join(',', Policies(query.Authorization!).Order(StringComparer.Ordinal)).ShouldEqual("IsSales,IsShopper,Special");
    }

    static IEnumerable<string> Policies(SemanticAuthorization requirement) => requirement switch
    {
        SemanticPolicyReference reference => [reference.Name],
        SemanticLogicalAuthorization logical => Policies(logical.Left).Concat(Policies(logical.Right)),
        _ => []
    };
}
