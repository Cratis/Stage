// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_from_a_directory : Specification
{
    const string Alpha =
        """
        module Alpha
          feature Things
            slice StateChange DoThing
              command DoThing
                id Uuid
        """;

    const string Beta =
        """
        module Beta
          feature Stuff
            slice StateChange DoStuff
              command DoStuff
                id Uuid
        """;

    string _directory = null!;
    EventModel _model = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-specs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_directory, "nested"));
        File.WriteAllText(Path.Combine(_directory, "alpha.play"), Alpha);

        // Placed in a sub-directory to prove the recursive **/*.play glob is used.
        File.WriteAllText(Path.Combine(_directory, "nested", "beta.play"), Beta);
    }

    async Task Because() => _model = await EventModelLoader.LoadFromDirectoryAsync(_directory);

    [Fact] void should_produce_a_single_collection() => _model.Collections.Count.ShouldEqual(1);
    [Fact] void should_merge_all_modules() => _model.Collections[0].Modules.Count.ShouldEqual(2);
    [Fact] void should_include_the_top_level_module() => _model.Collections[0].Modules.Any(module => module.Name == "Alpha").ShouldBeTrue();
    [Fact] void should_include_the_nested_module() => _model.Collections[0].Modules.Any(module => module.Name == "Beta").ShouldBeTrue();

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
