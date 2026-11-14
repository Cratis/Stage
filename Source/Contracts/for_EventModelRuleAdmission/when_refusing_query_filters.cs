// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_refusing_query_filters : given.a_modeled_rule
{
    void Establish() => _source = Source.Replace("      query ListRecords => Records[]", "      query ListRecords => Records[]\n        filter name String optional", StringComparison.Ordinal);

    [Fact] void should_name_the_query_filter_rule() => Refusal.Message.ShouldContain("query 'ListRecords' filter declarations");
    [Fact] void should_report_unsupported() => Refusal.Message.ShouldContain("STAGE-ESM-081: Unsupported");
}
