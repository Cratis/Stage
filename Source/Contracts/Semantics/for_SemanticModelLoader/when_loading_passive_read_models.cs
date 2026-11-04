// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_passive_read_models : Specification
{
    const string Source = """
        command reads protected
        module Stock
          feature Decisions
            slice StateChange Adjust
              readmodel Balance
                passive
                id String key
                quantity Int
              projection BuildBalance => Balance
                from Adjusted
                  id = $eventSourceId
                  quantity = quantity
                remove with Removed
              event Adjusted
                quantity Int
              event Removed
              command Adjust
                id String identifier
                quantity Int
                reads Balance by id
                produces Adjusted
                  for id
                  quantity = quantity
        """;

    string _root = null!;
    LoadedSemanticModel _loaded = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"stage-passive-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Stock.play"), Source);
    }

    async Task Because() => _loaded = await SemanticModelLoader.LoadFromPathAsync(_root);

    [Fact] void should_retain_the_compilers_explicit_passive_identity() => _loaded.PassiveReadModels.Single().ShouldEqual(_loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().ReadModels.Single().Id);
    [Fact] void should_preserve_protected_command_reads() => _loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Reads.Length.ShouldEqual(1);

    void Destroy() => Directory.Delete(_root, recursive: true);
}
#endif
