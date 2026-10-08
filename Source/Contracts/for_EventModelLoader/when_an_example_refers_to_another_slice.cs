// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_an_example_refers_to_another_slice : Specification
{
    Slice _source = null!;
    Slice _consumer = null!;

    void Because()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Projects
              feature Registration
                slice StateChange Register
                  event ProjectRegistered
                    name String
                  example Registered : ProjectRegistered
                    name = "project"
                slice StateChange Rename
                  event ProjectRegistered
                    name String
                  command RenameProject
                    name String
                  specification RenamingAProject
                    given Projects.Registration.Register.Registered
                    when RenameProject
                      name = "renamed"
            """);
        var slices = model.Collections.Single().Modules.Single().Features.Single().Slices;
        _source = slices.Single(slice => slice.Name == "Register");
        _consumer = slices.Single(slice => slice.Name == "Rename");
    }

    [Fact] void should_keep_the_cross_slice_name_qualified() => _consumer.Specifications.Single().Given.Single().Name.ShouldEqual("Projects.Registration.Register.ProjectRegistered");
    [Fact] void should_preserve_the_example_declaration_scope() => _consumer.Specifications.Single().Given.Single().EventId.ShouldEqual(_source.Events.Single().Id);
    [Fact] void should_not_resolve_to_the_same_named_event_in_the_use_scope() => _consumer.Specifications.Single().Given.Single().EventId.ShouldNotEqual(_consumer.Events.Single().Id);
}
