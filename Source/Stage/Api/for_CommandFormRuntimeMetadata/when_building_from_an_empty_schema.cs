// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Api.for_CommandFormRuntimeMetadata;

public class when_building_from_an_empty_schema : Specification
{
    CommandFormRuntimeCommand _result = null!;

    void Because() => _result = CommandFormRuntimeMetadata.FromSchema("RefreshDashboard", """{"type":"object","properties":{}}""");

    [Fact] void should_keep_the_parameterless_schema() => _result.Schema.ShouldEqual("""{"type":"object","properties":{}}""");
    [Fact] void should_expose_no_fields() => _result.Fields.ShouldBeEmpty();
}
