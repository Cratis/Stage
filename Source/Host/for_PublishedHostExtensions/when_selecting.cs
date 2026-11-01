// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedHostExtensions;

public class when_selecting
{
    sealed class Extension(string name) : IPublishedHostExtension
    {
        public string Name { get; } = name;

        public void ConfigureServices(PublishedHostContext context)
        {
        }

        public void MapEndpoints(WebApplication app, PublishedHostContext context)
        {
        }
    }

    [Fact] void should_compose_the_notes_backend_when_enabled() => PublishedHostExtensions.Select([new Extension("Notes")], true).Count.ShouldEqual(1);
    [Fact] void should_leave_the_notes_backend_out_when_disabled() => PublishedHostExtensions.Select([new Extension("Notes")], false).ShouldBeEmpty();
    [Fact] void should_always_compose_other_extensions() => PublishedHostExtensions.Select([new Extension("Other")], false).Count.ShouldEqual(1);
    [Fact] void should_refuse_notes_without_a_backend() => Assert.Throws<PublishedModeRefused>(() => PublishedHostExtensions.Select([], true));
}
