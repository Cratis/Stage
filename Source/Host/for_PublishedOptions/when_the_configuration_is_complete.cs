// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_the_configuration_is_complete : a_published_configuration
{
    IReadOnlyList<string> _problems = null!;

    void Because() => _problems = Options().Problems();

    [Fact] void should_have_no_problems() => _problems.ShouldBeEmpty();
}
