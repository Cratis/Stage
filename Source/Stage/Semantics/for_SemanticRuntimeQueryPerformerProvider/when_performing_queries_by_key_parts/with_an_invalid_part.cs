// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_queries_by_key_parts;

public class with_an_invalid_part : given.a_composite_key_runtime
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => await Perform("Total", new QueryArguments { ["invoiceId"] = "X", ["method"] = "cash" }));

    [Fact] void should_reject_the_query() => _error.ShouldBeOfExactType<SemanticQueryRejected>();
    [Fact] void should_name_the_concept_rule() => _error!.Message.ShouldEqual("Invoice id is too short");
}
