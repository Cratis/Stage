// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_naming_the_event_store : a_published_configuration
{
    [Fact] void should_derive_the_name_from_the_published_name() => Options().EventStoreFor("my-shop").ShouldEqual("app-my-shop");
    [Fact] void should_give_the_same_name_every_time() => Options().EventStoreFor("my-shop").ShouldEqual(Options().EventStoreFor("my-shop"));
    [Fact] void should_give_different_applications_different_names() => Options().EventStoreFor("my-shop").ShouldNotEqual(Options().EventStoreFor("other-shop"));

    [Fact]
    void should_use_the_configured_name_instead()
    {
        _values["Stage:Published:EventStore"] = " shop-store ";
        Options().EventStoreFor("my-shop").ShouldEqual("shop-store");
    }
}
