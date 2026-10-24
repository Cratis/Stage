// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_CommandFormSchemas;

public class when_synthesizing_reference_fields : Specification
{
    static readonly SemanticId ConceptId = SemanticId.Create(SemanticKind.Concept, "OrganisationId");
    static readonly SemanticProperty Key = new(SemanticId.Create(SemanticKind.Property, "organisationKey"), "organisationId", new(SemanticTypeReferenceKind.Concept, default, ConceptId, false, false), true);
    static readonly SemanticProperty Name = new(default, "name", new(SemanticTypeReferenceKind.Primitive, SemanticPrimitiveType.Text, default, false, false), false);
    static readonly SemanticReadModel Model = new(SemanticId.Create(SemanticKind.ReadModel, "Organisation"), "Organisation", [Key, Name]);
    static readonly SemanticKeyedQuery Query = new(default, "AllOrganisations", null, Model.Id, null, SemanticQueryCardinality.Many, SemanticQueryDelivery.Snapshot);
    static readonly SemanticProperty Reference = Key with { IsIdentifier = false };
    static readonly SourceLocation Location = new(3, 5, "opportunity.play");

    [Fact]
    public void should_annotate_the_shared_schema_with_the_query_key_and_label_but_no_route()
    {
        var reference = Schema(Application(), [Reference])["properties"]![Reference.Name]!["x-reference"]!;
        reference["query"]!.GetValue<string>().ShouldEqual("AllOrganisations");
        reference["value"]!.GetValue<string>().ShouldEqual("organisationId");
        reference["label"]!.GetValue<string>().ShouldEqual("name");
        reference.AsObject().ContainsKey("route").ShouldBeFalse();
    }

    [Theory]
    [InlineData("name", "title", "name")]
    [InlineData("displayName", "title", "title")]
    [InlineData("description", "displayName", "displayName")]
    [InlineData("description", "notes", "description")]
    public void should_choose_labels_in_priority_order(string first, string second, string expected)
    {
        var model = Model with { Properties = [Key, Name with { Name = first }, Name with { Name = second }] };
        Schema(Application([model]), [Reference])["properties"]![Reference.Name]!["x-reference"]!["label"]!.GetValue<string>().ShouldEqual(expected);
    }

    [Fact]
    public void should_use_the_key_as_label_if_there_is_no_string_property()
    {
        Schema(Application([Model with { Properties = [Key] }]), [Reference])["properties"]![Reference.Name]!["x-reference"]!["label"]!.GetValue<string>().ShouldEqual(Key.Name);
    }

    [Fact]
    public void should_prefer_the_model_named_after_the_identifier_concept()
    {
        var other = Model with { Id = SemanticId.Create(SemanticKind.ReadModel, "OrganisationSummary"), Name = "OrganisationSummary" };
        var schema = Schema(Application([other, Model], [Query with { Name = "Summaries", ReadModel = other.Id }, Query]), [Reference]);
        schema["properties"]![Reference.Name]!["x-reference"]!["query"]!.GetValue<string>().ShouldEqual(Query.Name);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_not_annotate_the_commands_own_identity_or_generated_members(bool generated)
    {
        var property = Reference with { IsIdentifier = !generated, IsGenerated = generated };
        var diagnostics = new ConversionDiagnostics();
        var schema = Schema(Application(), [property], diagnostics);
        (schema["properties"]?[property.Name]?["x-reference"] is null).ShouldBeTrue();
        diagnostics.All.ShouldBeEmpty();
    }

    [Fact]
    public void should_still_describe_a_generated_member_so_its_type_can_be_read()
    {
        var property = Reference with { IsGenerated = true };
        var schema = Schema(Application(), [property], new ConversionDiagnostics());
        schema["properties"]![property.Name]!.AsObject().ContainsKey("type").ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_text_and_report_information_if_no_unique_pair_exists(bool ambiguous)
    {
        var queries = ambiguous ? new[] { Query, Query with { Name = "OtherOrganisations" } } : [];
        var diagnostics = new ConversionDiagnostics();
        var schema = Schema(Application(queries: queries), [Reference], diagnostics);
        (schema["properties"]![Reference.Name]!["x-reference"] is null).ShouldBeTrue();
        var note = diagnostics.All.Single();
        note.Code.ShouldEqual("STAGE-SCENE-REFERENCE-001");
        note.Severity.ShouldEqual(DiagnosticSeverity.Information);
        note.Path.ShouldEqual("RegisterOpportunity.organisationId");
        note.Location.ShouldEqual(Location);
        note.Message.Contains("Organisation", StringComparison.Ordinal).ShouldBeTrue();
        if (ambiguous)
        {
            note.Message.Contains("AllOrganisations", StringComparison.Ordinal).ShouldBeTrue();
            note.Message.Contains("OtherOrganisations", StringComparison.Ordinal).ShouldBeTrue();
        }
        diagnostics.HasErrors.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_reject_queries_requiring_an_argument_or_filter(bool filter)
    {
        var argument = new SemanticReadModelQueryArgument(default, "region", Name.Type);
        var query = filter ? Query with { Filters = [new(default, "region", Name.Type, Name.Id)] } : Query with { Argument = argument };
        (Schema(Application(queries: [query]), [Reference])["properties"]![Reference.Name]!["x-reference"] is null).ShouldBeTrue();
    }

    [Fact]
    public void should_allow_optional_query_filters()
    {
        var query = Query with { Filters = [new(default, "region", Name.Type with { IsOptional = true }, Name.Id)] };
        (Schema(Application(queries: [query]), [Reference])["properties"]![Reference.Name]!["x-reference"] is not null).ShouldBeTrue();
    }

    [Fact]
    public void should_reject_single_instance_queries()
    {
        (Schema(Application(queries: [Query with { Cardinality = SemanticQueryCardinality.ZeroOrOne }]), [Reference])["properties"]![Reference.Name]!["x-reference"] is null).ShouldBeTrue();
    }

    [Fact]
    public void should_not_infer_references_from_an_id_suffix_on_an_ordinary_concept()
    {
        var model = Model with { Properties = [Key with { IsIdentifier = false }, Name] };
        var diagnostics = new ConversionDiagnostics();
        (Schema(Application([model]), [Reference], diagnostics)["properties"]![Reference.Name]!["x-reference"] is null).ShouldBeTrue();
        diagnostics.All.ShouldBeEmpty();
    }

    static JsonObject Schema(SemanticApplication application, SemanticProperty[] properties, ConversionDiagnostics? diagnostics = null) =>
        JsonNode.Parse(CommandFormSchemas.For(application, properties, diagnostics ?? new(), "RegisterOpportunity", Location)!)!.AsObject();

    static SemanticApplication Application(SemanticReadModel[]? models = null, SemanticKeyedQuery[]? queries = null)
    {
        var slice = new SemanticSlice(default, "Organisations", SemanticSliceKind.StateView, [], [], [.. models ?? [Model]], [], [.. queries ?? [Query]], []);
        return new(
            default,
            "Crm",
            [new(ConceptId, "OrganisationId", SemanticPrimitiveType.Uuid, [], [])],
            [],
            [new(default, "Crm", [new(default, "Organisations", [], [slice])])]);
    }
}
