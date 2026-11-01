// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_reporting_typed_locations : given.an_inventory
{
    [Theory]
    [InlineData(null)]
    [InlineData("orders.play")]
    [InlineData("models/Café e\u0301/orders.play")]
    void should_emit_the_same_lossless_location_on_owner_and_gap(string? path)
    {
        var construct = _root with { ModelPath = path, Degradations = [_root.Degradations.Single() with { ModelPath = path }] };
        var plan = Plan(ArtifactRenderInventory.Create([construct]));
        using var json = JsonDocument.Parse(ArtifactRenderReports.Create(plan, _context).Completeness.Bytes.AsMemory());
        var owner = json.RootElement.GetProperty("inventory").GetProperty("constructs")[0];
        JsonSerializer.Serialize(owner.GetProperty("semanticLocator")).ShouldEqual("{\"kind\":\"Concept\",\"parts\":[{\"kind\":\"Application\",\"key\":\"app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994\"},{\"kind\":\"Declaration\",\"key\":\"Owner1\"}]}");
        owner.GetProperty("modelPath").GetString().ShouldEqual(path);
        var gap = owner.GetProperty("degradations")[0];
        JsonSerializer.Serialize(gap.GetProperty("semanticLocator")).ShouldEqual(JsonSerializer.Serialize(owner.GetProperty("semanticLocator")));
        gap.GetProperty("modelPath").GetString().ShouldEqual(path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/orders.play")]
    [InlineData("C:/orders.play")]
    [InlineData("../orders.play")]
    [InlineData("models/../orders.play")]
    [InlineData("models//orders.play")]
    [InlineData("models\\orders.play")]
    [InlineData("models/orders.play/")]
    void should_reject_nonportable_or_directory_paths(string path) => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([_rendered with { ModelPath = path }]));

    [Fact]
    void should_not_fabricate_a_path_when_source_is_unavailable()
    {
        var construct = _root with { ModelPath = null, Degradations = [_root.Degradations.Single() with { ModelPath = null }] };
        var reports = ArtifactRenderReports.Create(Plan(ArtifactRenderInventory.Create([construct])), _context);
        Encoding.UTF8.GetString(reports.Unsupported.Bytes.AsSpan()).Contains("Source unavailable", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    void should_reject_a_different_gap_locator() => Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderInventory.Create([_root with { Degradations = [_root.Degradations.Single() with { SemanticLocator = Locator(2) }] }]));

    [Fact]
    void should_distinguish_kind_and_parent_child_boundaries_in_ordering_and_digest()
    {
        var application = ApplicationIdentity.Create("Projects");
        var single = SemanticAddress.ForSlice(application, "Orders", ["A/B"], "Place");
        var nested = SemanticAddress.ForSlice(application, "Orders", ["A", "B"], "Place");
        var command = SemanticAddress.ForCommand(single, "Same");
        var query = SemanticAddress.ForQuery(single, "Same");
        Assert.NotEqual(0, ArtifactRenderSemanticLocatorComparer.Instance.Compare(single, nested));
        Assert.NotEqual(0, ArtifactRenderSemanticLocatorComparer.Instance.Compare(command, query));
        var first = _rendered with { SemanticLocator = command };
        var second = _rendered with { Identity = Id(5), SemanticLocator = query };
        var forward = ArtifactRenderInventory.Create([first, second]);
        var reverse = ArtifactRenderInventory.Create([second, first]);
        forward.Digest.ShouldEqual(reverse.Digest);
        ArtifactRenderInventory.Create([first]).Digest.ShouldNotEqual(ArtifactRenderInventory.Create([first with { SemanticLocator = query }]).Digest);
    }

    [Fact]
    void should_preserve_unicode_and_escape_report_markup_without_altering_semantic_keys()
    {
        var locator = SemanticAddress.ForConcept(ApplicationIdentity.Create("Projects"), "Café <&> `body`");
        var construct = _root with { SemanticLocator = locator, Degradations = [_root.Degradations.Single() with { SemanticLocator = locator }] };
        var reports = ArtifactRenderReports.Create(Plan(ArtifactRenderInventory.Create([construct])), _context);
        using var json = JsonDocument.Parse(reports.Completeness.Bytes.AsMemory());
        json.RootElement.GetProperty("inventory").GetProperty("constructs")[0].GetProperty("semanticLocator").GetProperty("parts")[1].GetProperty("key").GetString().ShouldEqual(locator.Name);
        Encoding.UTF8.GetString(reports.Unsupported.Bytes.AsSpan()).Contains("Café &lt;&amp;&gt; &#96;body&#96;", StringComparison.Ordinal).ShouldBeTrue();
    }
}
