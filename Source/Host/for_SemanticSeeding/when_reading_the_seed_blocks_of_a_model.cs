// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeding;

public class when_reading_the_seed_blocks_of_a_model : Specification
{
    const string Model = """
        module Shop
          feature Catalog
            slice StateChange ListProduct
              command ListProduct
                sku   String identifier
                title String
                produces ProductListed
                  for sku
                  sku   = sku
                  title = title
              event ProductListed
                sku    String
                title  String
                price  Int
                tags   String[]
                size   String

        seed
          for "ACM-1"
            ProductListed
              sku   = "ACM-1"
              title = "Acme mug"
              price = 49
              tags  = ["kitchen", "mug"]
              size  = null
          for "ACM-2"
            ProductListed
              sku   = "ACM-2"
              title = "Acme pot"
              price = 129
        seed
          for "ACM-3"
            ProductListed
              sku   = "ACM-3"
              title = "Acme pan"
        """;

    string _directory = null!;
    IReadOnlyList<SeedGroup> _groups = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-seed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "shop.play"), Model);
    }

    void Because() => _groups = SemanticSeeding.Load(_directory);

    [Fact] void should_read_one_group_per_event_source_across_blocks() => _groups.Select(group => group.EventSourceId).ShouldEqual(["ACM-1", "ACM-2", "ACM-3"]);
    [Fact] void should_keep_the_event_name() => _groups[0].Events[0].Event.ShouldEqual("ProductListed");
    [Fact] void should_carry_text_values() => _groups[0].Events[0].Content["title"]!.GetValue<string>().ShouldEqual("Acme mug");
    [Fact] void should_carry_whole_numbers_as_numbers() => _groups[0].Events[0].Content["price"]!.GetValue<long>().ShouldEqual(49);
    [Fact] void should_carry_lists() => _groups[0].Events[0].Content["tags"]!.AsArray().Select(item => item!.GetValue<string>()).ShouldEqual(["kitchen", "mug"]);
    [Fact] void should_carry_null() => _groups[0].Events[0].Content["size"].ShouldBeNull();
}
