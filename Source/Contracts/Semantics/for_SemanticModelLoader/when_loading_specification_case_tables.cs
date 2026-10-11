// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader.given;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_specification_case_tables : a_source_tree
{
    async Task Establish() => await File.WriteAllTextAsync(Path.Combine(_root, "nested", "source.play"), """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId String identifier
                name String
                produces ProjectRegistered
                  for projectId
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
        """);

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Projects");

    [Fact] void should_load_the_table() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_each_case_origin() => _result.Loaded!.SpecificationOrigins.Values.Select(origin => origin.Case!.Name).ShouldContainOnly("Small", "Large");
    [Fact] void should_preserve_the_authored_table_name() => _result.Loaded!.SpecificationOrigins.Values.All(origin => origin.Authored.Name == "Registering").ShouldBeTrue();
    [Fact] void should_key_origins_by_the_derived_specification_identity() => _result.Loaded!.SpecificationOrigins.Keys.ShouldContainOnly(_result.Loaded.Plan.Specifications.Keys);
    [Fact] void should_preserve_the_effective_case_values() => _result.Loaded!.SpecificationOrigins.Values.SelectMany(origin => origin.Steps).SelectMany(step => step.Values).Where(value => value.Origin == SpecificationValueOrigin.Case).Select(value => ((LiteralExpressionSyntax)value.Value).Value).Distinct().ShouldContainOnly("small", "large");
}
#endif
