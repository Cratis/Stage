// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Host.Workbench.for_WorkbenchTransformer.when_transforming_the_response;

/// <summary>
/// A play session is reached through its caller's own proxy on a path prefix, so the prefix the browser used is
/// the caller's plus the Workbench's - and that whole path, not just the Workbench's own segment, is what the
/// page has to be told about.
/// </summary>
public class and_the_caller_reached_it_through_a_proxy : given.a_workbench_response
{
    const string PublicPrefix = "/api/play/2f1c9a3e-0000-4000-8000-000000000001/workbench";

    string _body;

    async Task Because() => (_body, _) = await Forward(PublicPrefix, Index, "text/html");

    [Fact] void should_tell_the_page_the_whole_public_path() => Assert.Contains($"name=\"base-path\" content=\"{PublicPrefix}\"", _body, StringComparison.Ordinal);
    [Fact] void should_prefix_assets_with_the_whole_public_path() => Assert.Contains($"src=\"{PublicPrefix}/index-a1b2c3.js\"", _body, StringComparison.Ordinal);
}
