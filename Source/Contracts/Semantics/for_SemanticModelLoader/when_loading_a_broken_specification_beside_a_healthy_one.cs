// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_broken_specification_beside_a_healthy_one : given.a_source_tree
{
    string _path = null!;
    LoadedSemanticModel _loaded = null!;
    InvalidSemanticModel? _strictFailure;

    async Task Establish()
    {
        _path = Path.Combine(_root, "nested", "source.play");
        await File.WriteAllTextAsync(_path, """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId String identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
                  specification Healthy
                    when RegisterProject
                      projectId = "first"
                    then ProjectRegistered
                      for "first"
                  specification Broken
                    when RegisterProject
                      projectId = "second"
                    then NoSuchEvent
                      for "second"
            """);
    }

    async Task Because()
    {
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_path, null, null, allowRefusedSpecifications: true);
        try
        {
            await SemanticModelLoader.LoadFromPathAsync(_path, null, null, allowRefusedSpecifications: false);
        }
        catch (InvalidSemanticModel failure)
        {
            _strictFailure = failure;
        }
    }

    [Fact] void should_list_only_the_broken_specification() => _loaded.RefusedSpecifications.Length.ShouldEqual(1);
    [Fact] void should_keep_the_original_error() => _loaded.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_select_the_refusal_for_an_empty_scope() => _loaded.RefusalsFor([]).Length.ShouldEqual(1);
    [Fact] void should_select_the_refusal_for_the_application() => _loaded.RefusalsFor([_loaded.Model.Application.Id]).Length.ShouldEqual(1);
    [Fact] void should_select_the_refusal_for_its_own_identity() => _loaded.RefusalsFor([_loaded.RefusedSpecifications.Single().Id]).Length.ShouldEqual(1);
    [Fact] void should_still_fail_when_strict() => _strictFailure.ShouldNotBeNull();
}
#endif
