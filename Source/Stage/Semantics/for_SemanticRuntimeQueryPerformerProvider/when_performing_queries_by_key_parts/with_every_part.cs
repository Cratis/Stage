// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.when_performing_queries_by_key_parts;

public class with_every_part : given.a_composite_key_runtime
{
    async Task Because() => await Perform("Total", new QueryArguments { ["invoiceId"] = "INV-1", ["method"] = "cash" });

    [Fact] void should_expose_one_parameter_per_part() => _performer.Parameters.Select(parameter => parameter.Name).ShouldContainOnly("invoiceId", "method");
    [Fact] void should_answer_the_instance_whose_parts_all_match() => ((DynamicReadModel)_result!).Values["total"].GetDecimal().ShouldEqual(8m);
    [Fact] void should_identify_the_instance_by_its_parts() => ((DynamicReadModel)_result!).Id.ShouldEqual("""{"invoiceId":"INV-1","method":"cash"}""");
}
