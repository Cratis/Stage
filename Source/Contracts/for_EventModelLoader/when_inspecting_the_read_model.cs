// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_inspecting_the_read_model : given.a_compiled_invoicing_model
{
    ReadModelDefinition _readModel = null!;
    JsonElement _properties;

    void Because()
    {
        _readModel = _model.Collections[0].Modules[0].Features[0].Slices.Single(slice => slice.Name == "InvoiceList").ReadModel!;
        using var schema = JsonDocument.Parse(_readModel.Schema);
        _properties = schema.RootElement.GetProperty("properties").Clone();
    }

    [Fact] void should_name_the_read_model_after_the_projection_target() => _readModel.Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_include_the_mapping_target_properties() => _properties.TryGetProperty("invoiceNumber", out _).ShouldBeTrue();
    [Fact] void should_infer_the_status_type_from_the_event() => _properties.GetProperty("status").GetProperty("type").GetString().ShouldEqual("string");
    [Fact] void should_leave_unknown_properties_open() => _properties.GetProperty("lineCount").EnumerateObject().Any().ShouldEqual(false);
    [Fact] void should_include_the_query_filter_property() => _properties.TryGetProperty("status", out _).ShouldBeTrue();
    [Fact] void should_build_a_projection() => Projection.From.ContainsKey("InvoiceRegistered").ShouldBeTrue();
    [Fact] void should_map_the_from_key() => Projection.From["InvoiceRegistered"].Key.ShouldEqual("invoiceId");
    [Fact] void should_default_a_missing_key_to_null() => Projection.From["InvoiceStatusChanged"].Key.ShouldBeNull();
    [Fact] void should_quote_a_literal_string_mapping() => Expression("InvoiceRegistered", "status").ShouldEqual("\"draft\"");
    [Fact] void should_keep_a_path_mapping() => Expression("InvoiceStatusChanged", "status").ShouldEqual("status");
    [Fact] void should_default_automap_to_enabled() => Projection.AutoMap.ShouldEqual(ProjectionAutoMap.Enabled);

    ProjectionDefinition Projection => _readModel.Projection!;

    string Expression(string @event, string property) =>
        Projection.From[@event].Properties.Single(mapping => mapping.Property == property).Expression;
}
