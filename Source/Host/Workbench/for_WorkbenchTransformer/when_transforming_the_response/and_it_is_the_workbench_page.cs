// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Workbench.for_WorkbenchTransformer.when_transforming_the_response;

public class and_it_is_the_workbench_page : given.a_workbench_response
{
    const string PublicPrefix = "/workbench";

    string _body;
    bool _copied;

    async Task Because() => (_body, _copied) = await Forward(PublicPrefix, Index, "text/html");

    [Fact] void should_write_the_body_itself() => _copied.ShouldBeFalse();
    [Fact] void should_tell_the_page_where_it_is_served_from() => Assert.Contains($"name=\"base-path\" content=\"{PublicPrefix}\"", _body, StringComparison.Ordinal);
    [Fact] void should_leave_no_unset_base_path_behind() => Assert.DoesNotContain(BasePathMetaPattern, _body, StringComparison.Ordinal);
    [Fact] void should_prefix_script_references() => Assert.Contains($"src=\"{PublicPrefix}/index-a1b2c3.js\"", _body, StringComparison.Ordinal);
    [Fact] void should_prefix_link_references() => Assert.Contains($"href=\"{PublicPrefix}/favicon.svg\"", _body, StringComparison.Ordinal);
    [Fact] void should_leave_no_root_absolute_reference_behind() => Assert.DoesNotContain("src=\"/index", _body, StringComparison.Ordinal);
}
