// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Extensions.for_Extension.given;
using Xunit;

namespace Cratis.Stage.Extensions.for_Extension;

public class when_running_the_conformance_suite : Specification
{
    int _cases;
    IReadOnlyList<string> _failures = null!;

    async Task Because() => (_cases, _failures) = await ConformanceSuite.Run(ConformanceExtension.Create());

    [Fact] void should_run_every_case() => _cases.ShouldBeGreaterThan(10);
    [Fact] void should_pass_every_case() => string.Join(Environment.NewLine, _failures).ShouldEqual(string.Empty);
}
