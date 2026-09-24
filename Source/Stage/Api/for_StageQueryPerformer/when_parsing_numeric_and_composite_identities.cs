// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_parsing_numeric_and_composite_identities : given.a_stage_query_performer
{
    [Theory]
    [InlineData("""{"id":42,"name":"Test item"}""", "42")]
    [InlineData("""{"id":42.0,"name":"Test item"}""", "42")]
    [InlineData("""{"id":true,"name":"Test item"}""", "True")]
    [InlineData("""{"id":{"region":"west","number":42},"name":"Test item"}""", "42_west")]
    [InlineData("""{"id":{"second":false,"first":"foo"},"name":"Test item"}""", "foo_False")]
    public void should_address_the_instance_by_its_kernel_key(string document, string key)
    {
        var instance = (DynamicReadModel)Assert.Single(_performer.Parse([document]));
        instance.Id.ShouldEqual(key);
        instance.Values["name"].GetString().ShouldEqual("Test item");
        instance.Values.ContainsKey("id").ShouldBeFalse();
    }
}
