// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_queries_by_key_parts;

public class without_a_part : given.a_composite_key_runtime
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => await Perform("Total", new QueryArguments { ["invoiceId"] = "INV-1" }));

    [Fact] void should_reject_instead_of_matching_on_the_supplied_part() => _error.ShouldBeOfExactType<SemanticQueryRejected>();
}
