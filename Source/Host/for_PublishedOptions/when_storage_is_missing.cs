// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_storage_is_missing : a_published_configuration
{
    PublishedModeRefused _refused = null!;

    void Establish() => _values.Remove("Stage:Published:Storage:ConnectionString");

    void Because() => _refused = Assert.Throws<PublishedModeRefused>(() => Options().EnsureComplete());

    [Fact] void should_refuse_to_start() => (_refused is not null).ShouldBeTrue();
    [Fact] void should_name_the_setting() => _refused.Message.ShouldContain("Stage:Published:Storage:ConnectionString");
}
