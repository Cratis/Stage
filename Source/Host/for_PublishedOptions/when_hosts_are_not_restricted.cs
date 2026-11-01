// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_hosts_are_not_restricted : a_published_configuration
{
    IReadOnlyList<string> _problems = null!;

    void Establish() => _values["AllowedHosts"] = "*";

    void Because() => _problems = Options().Problems();

    [Fact] void should_refuse_the_wildcard() => _problems.Any(problem => problem.Contains("AllowedHosts")).ShouldBeTrue();
}
