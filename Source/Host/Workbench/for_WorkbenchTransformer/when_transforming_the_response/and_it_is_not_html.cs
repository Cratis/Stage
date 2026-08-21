// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Workbench.for_WorkbenchTransformer.when_transforming_the_response;

/// <summary>
/// Only the page needs rewriting. Assets, API responses and event streams have to be left to stream through -
/// buffering them to look for something that is not there would break the observable queries the Workbench lives on.
/// </summary>
public class and_it_is_not_html : given.a_workbench_response
{
    string _body;
    bool _copied;

    async Task Because() => (_body, _copied) = await Forward("/workbench", """{"src":"/not-a-page"}""", "application/json");

    [Fact] void should_leave_the_body_to_the_forwarder() => _copied.ShouldBeTrue();
    [Fact] void should_not_write_anything_itself() => Assert.Empty(_body);
}
