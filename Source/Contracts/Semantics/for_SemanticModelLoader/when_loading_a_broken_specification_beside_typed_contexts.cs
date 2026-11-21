// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_broken_specification_beside_typed_contexts : given.a_source_tree
{
    const string Source = """
        concept Code : String
          validate csharp
            ```csharp
            return true;
            ```
        module Projects
          feature Lookup
            slice StateChange RegisterCode
              command RegisterCode
                id Uuid identifier
                code Code
                produces CodeRegistered
                  for id
              event CodeRegistered
              specification Broken
                when RegisterCode
                  id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  code = "a"
                then NoSuchEvent
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    LoadedSemanticModel _strict = null!;
    LoadedSemanticModel _lenient = null!;

    async Task Establish()
    {
        var path = Path.Combine(_root, "nested", "source.play");
        await File.WriteAllTextAsync(path, Source);
        _lenient = await SemanticModelLoader.LoadFromPathAsync(path, null, null, allowRefusedSpecifications: true);
        await File.WriteAllTextAsync(path, Source.Replace("NoSuchEvent", "CodeRegistered", StringComparison.Ordinal));
        _strict = await SemanticModelLoader.LoadFromPathAsync(path, null, null, allowRefusedSpecifications: false);
    }

    [Xunit.Fact] void should_refuse_the_broken_specification() => _lenient.RefusedSpecifications.Length.ShouldEqual(1);
    [Xunit.Fact] void should_have_typed_context_descriptors_when_nothing_is_refused() => _strict.TypedContextDescriptors.Length.ShouldBeGreaterThan(0);
    [Xunit.Fact] void should_keep_the_typed_context_descriptors() => _lenient.TypedContextDescriptors.Length.ShouldEqual(_strict.TypedContextDescriptors.Length);
}
#endif
