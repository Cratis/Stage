// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Samples.Customization;

/// <summary>
/// Retrieves names from an external product catalog without contacting it during host registration.
/// </summary>
/// <param name="client">The HTTP client supplied by dependency injection.</param>
/// <param name="options">The product-owned catalog settings.</param>
public sealed class ProductCatalog(HttpClient client, IOptions<ProductCatalogOptions> options) : IProductCatalog
{
    /// <inheritdoc/>
    public async Task<string?> GetName(string id)
    {
        var address = new Uri(options.Value.BaseAddress, $"products/{Uri.EscapeDataString(id)}");
        using var response = await client.GetAsync(address);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDetails>();

        return product?.Name;
    }

    sealed record ProductDetails(string Name);
}

/// <summary>
/// Reads product names from an external catalog.
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Retrieves one product name.
    /// </summary>
    /// <param name="id">The catalog identifier to look up.</param>
    /// <returns>The name when the product exists.</returns>
    Task<string?> GetName(string id);
}

/// <summary>
/// Configures the address used to reach the product catalog.
/// </summary>
public sealed class ProductCatalogOptions
{
    /// <summary>
    /// Gets or sets the local sample catalog endpoint; set ProductCatalog:BaseAddress for a real catalog.
    /// </summary>
    public Uri BaseAddress { get; set; } = new("http://localhost:5050/");
}
