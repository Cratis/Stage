// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxNotes;

public class when_reading_the_configuration
{
    static SandboxNotes Read(params (string Key, string Value)[] values) => SandboxNotes.Read(
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(pair => pair.Key, pair => (string?)pair.Value)).Build());

    [Fact] void should_be_off_by_default() => Read().Enabled.ShouldBeFalse();
    [Fact] void should_be_off_for_a_value_that_is_not_a_boolean() => Read(("Stage:Sandbox:Notes:Enabled", "yes")).Enabled.ShouldBeFalse();
    [Fact] void should_be_on_when_enabled() => Read(("Stage:Sandbox:Notes:Enabled", "true")).Enabled.ShouldBeTrue();
    [Fact] void should_not_be_turned_on_by_the_published_switch() => Read(("Stage:Published:Notes:Enabled", "true")).Enabled.ShouldBeFalse();
    [Fact] void should_read_the_references_file() => Read(("Stage:Sandbox:Notes:ModelReferencesFile", "refs.json")).ModelReferencesFile.ShouldEqual("refs.json");

    [Fact]
    void should_describe_the_identity_for_the_feedback_button()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Notes:ApplicationName"] = "Ledger",
            ["Notes:PublicationId"] = "play-1"
        }).Build();
        var json = System.Text.Json.JsonSerializer.Serialize(SandboxNotes.Describe(configuration, [new Cratis.Stage.Publishing.ModelReference("slice", "Shop.Orders.Place", "id-1")]));
        json.ShouldContain("\"notesEnabled\":true");
        json.ShouldContain("\"publicationId\":\"play-1\"");
        json.ShouldContain("\"Id\":\"id-1\"");
    }
}
