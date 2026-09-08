// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Introspection;
using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_discovering_canonical_routes : given.a_routed_model
{
    IReadOnlyList<CommandIntrospectionMetadata> _discoveredCommands = [];
    IReadOnlyList<QueryIntrospectionMetadata> _discoveredQueries = [];
    JsonDocument _document = null!;
    RouteEndpoint[] _aliases = [];
    int _status;

    async Task Because()
    {
        MapModel(RouteModels.WithFeatures(
            RouteModels.Feature("Checkout", [RouteModels.Command("PlaceOrder"), RouteModels.Command("CancelOrder")]),
            RouteModels.Feature("Invoices", [RouteModels.Command("RegisterInvoice")]),
            RouteModels.Feature("Reports", [RouteModels.Query("Current"), RouteModels.Query("Archive"), RouteModels.Query("InvoiceReport", "Invoice")])));
        _discoveredCommands = _introspection.Commands;
        _discoveredQueries = _introspection.Queries;
        _aliases = [.. Endpoints().Where(endpoint => Name(endpoint).EndsWith(".StageLegacy", StringComparison.Ordinal))];
        var response = await Request("GET", "/openapi/v1.json");
        _status = response.Status;
        _document = JsonDocument.Parse(response.Body);
    }

    void Destroy() => _document?.Dispose();

    static string Name(RouteEndpoint endpoint) => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName ?? string.Empty;

    bool IsCanonical(string method, string path) => Endpoints().Any(endpoint =>
        endpoint.RoutePattern.RawText == path &&
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains(method) &&
        !Name(endpoint).EndsWith(".StageLegacy", StringComparison.Ordinal));

    bool AliasCopiesMetadata(RouteEndpoint alias)
    {
        var originalName = Name(alias)[..^".StageLegacy".Length];
        var canonical = Endpoints().Single(endpoint => Name(endpoint) == originalName);
        return alias.Metadata.GetMetadata<IAcceptsMetadata>()?.RequestType == canonical.Metadata.GetMetadata<IAcceptsMetadata>()?.RequestType &&
            alias.Metadata.GetMetadata<IProducesResponseTypeMetadata>()?.Type == canonical.Metadata.GetMetadata<IProducesResponseTypeMetadata>()?.Type &&
            (alias.Metadata.GetMetadata<IAllowAnonymous>() is not null) == (canonical.Metadata.GetMetadata<IAllowAnonymous>() is not null) &&
            alias.Metadata.GetMetadata<ITagsMetadata>()!.Tags.SequenceEqual(canonical.Metadata.GetMetadata<ITagsMetadata>()!.Tags);
    }

    [Fact] void should_share_the_effective_options_instance_with_introspection() => ReferenceEquals(_app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value, _app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.GeneratedApis).ShouldBeTrue();
    [Fact] void should_discover_every_canonical_command() => (_discoveredCommands.Count == 3 && _discoveredCommands.All(command => IsCanonical("POST", command.Route))).ShouldBeTrue();
    [Fact] void should_discover_every_query_identity_before_url_deduplication() => (_discoveredQueries.Count == 6 && _discoveredQueries.All(query => IsCanonical("GET", query.Route) && IsCanonical("QUERY", query.Route))).ShouldBeTrue();
    [Fact] void should_never_advertise_an_extra_stage_segment() => _discoveredCommands.Select(command => command.Route).Concat(_discoveredQueries.Select(query => query.Route)).All(path => !path.Contains("/stage/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_generate_openapi_successfully() => _status.ShouldEqual(200);
    [Fact] void should_describe_exactly_the_canonical_paths() => _document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).Order(StringComparer.Ordinal).SequenceEqual(_surface.Operations.Select(operation => operation.CanonicalPath).Distinct().Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_describe_separate_same_named_command_operations() => _document.RootElement.GetProperty("paths").GetProperty("/api/orders/checkout/place-order/do-it").GetProperty("post").GetProperty("operationId").GetString().ShouldEqual("ExecuteStage.Orders.Checkout.PlaceOrder.DoIt");
    [Fact] void should_describe_the_other_same_named_command_operation() => _document.RootElement.GetProperty("paths").GetProperty("/api/orders/checkout/cancel-order/do-it").GetProperty("post").GetProperty("operationId").GetString().ShouldEqual("ExecuteStage.Orders.Checkout.CancelOrder.DoIt");
    [Fact] void should_have_compatibility_aliases_to_inspect() => _aliases.ShouldNotBeEmpty();
    [Fact] void should_exclude_every_alias_from_api_description() => _aliases.All(endpoint => endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription == true).ShouldBeTrue();
    [Fact] void should_copy_alias_body_response_authorization_and_tag_metadata() => _aliases.All(AliasCopiesMetadata).ShouldBeTrue();
    [Fact] void should_preserve_arcs_hidden_query_transport_metadata() => Endpoints().Where(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("QUERY")).All(endpoint => endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>()?.ExcludeFromDescription == true).ShouldBeTrue();
}
