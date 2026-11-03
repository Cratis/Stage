// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_model_without_authentication : Specification
{
    LoadedSemanticModel _loaded = null!;
    string _root = null!;

    async Task Establish()
    {
        _root = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-realization")).FullName;
        File.WriteAllText(Path.Combine(_root, "shop.play"), """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId Uuid identifier
                    produces ProjectRegistered
                      for projectId
                  event ProjectRegistered
            """);
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_root, null, "Shop");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_root);

    [Fact] void should_have_nothing_to_realize() => _loaded.Realization.IsEmpty.ShouldBeTrue();
}
#endif
