// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_expanding_specification_case_tables : Specification
{
    const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  name = name
              event ProjectRegistered
                name String
              specification Registering
                parameter name String
                case Small name = "small"
                case Large name = "large"
                when RegisterProject
                  projectId = "project-1"
                  name = case.name
                then ProjectRegistered
                  name = case.name
        """;

    Slice _slice = null!;

    void Because() => _slice = EventModelLoader.LoadFromSource(Source).Collections.Single().Modules.Single().Features.Single().Slices.Single();

    [Fact] void should_accept_the_table_through_the_event_model_visitor() => _slice.Specifications.Select(specification => specification.Name).ShouldContainOnly("Registering_Small", "Registering_Large");
    [Fact] void should_substitute_command_values() => Values("Registering_Small", true).ShouldEqual("small");
    [Fact] void should_substitute_event_values() => Values("Registering_Large", false).ShouldEqual("large");

    string? Values(string name, bool command)
    {
        var specification = _slice.Specifications.Single(specification => specification.Name == name);
        using var document = JsonDocument.Parse(command ? specification.When!.Values : specification.ThenEvents.Single().Values);

        return document.RootElement.GetProperty("name").GetString();
    }
}
