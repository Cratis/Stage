// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds;

public class when_reading_seeds_from_a_folder : a_seeded_model
{
    string _folder = null!;
    SemanticSeedPlan _seeds = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"stage-seeds-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "model.play"), Model);
        File.WriteAllText(Path.Combine(_folder, "seed.play"), """
            seed
              for "mug"
                ProductListed
                  name = "Stoneware mug"
                  price = 12.5
                  featured = false
                  tags = []
            """);
        Compile(string.Empty);
    }

    void Because() => _seeds = SemanticSeeds.Read(_folder, _plan);

    void Destroy() => Directory.Delete(_folder, true);

    [Fact] void should_find_the_seed_block_in_another_file() => _seeds.Groups.Single().Facts.Length.ShouldEqual(1);
    [Fact] void should_not_report_anything() => _seeds.Diagnostics.ShouldBeEmpty();
}
