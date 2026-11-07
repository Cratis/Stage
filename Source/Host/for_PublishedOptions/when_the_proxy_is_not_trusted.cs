// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_the_proxy_is_not_trusted : a_published_configuration
{
    IReadOnlyList<string> _problems = null!;

    void Establish()
    {
        _values.Remove("Stage:Published:TrustedProxies:0");
        _values.Remove("Stage:Published:TrustedProxies:1");
        _values["Stage:Published:TrustedProxies:0"] = "not-an-address";
    }

    void Because() => _problems = Options().Problems();

    [Fact] void should_refuse_a_malformed_proxy() => _problems.Any(problem => problem.Contains("not-an-address")).ShouldBeTrue();
}
