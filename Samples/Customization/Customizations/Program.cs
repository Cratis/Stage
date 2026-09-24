// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Samples.Customization;

/// <summary>
/// Connects the generated application to the product-owned catalog adapter.
/// </summary>
public partial class Program
{
    static partial void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ProductCatalogOptions>(builder.Configuration.GetSection("ProductCatalog"));
        builder.Services.AddHttpClient<IProductCatalog, ProductCatalog>();
    }

    static partial void ConfigureApplication(WebApplication app) =>
        app.MapGet("/api/catalog/products/{id}", async (string id, IProductCatalog catalog) =>
            await catalog.GetName(id) is { } name ? Results.Ok(name) : Results.NotFound());
}
