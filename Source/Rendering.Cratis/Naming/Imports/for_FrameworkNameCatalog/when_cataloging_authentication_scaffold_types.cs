// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_cataloging_authentication_scaffold_types : Specification
{
    FrameworkNameCatalog _catalog = null!;

    void Because() => _catalog = FrameworkNameCatalog.Current;

    [Theory]
    [InlineData("Microsoft.AspNetCore.Authentication", "AuthenticationOptions", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication", "AuthenticationProperties", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication.Cookies", "CookieAuthenticationDefaults", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication.Cookies", "CookieAuthenticationOptions", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication.JwtBearer", "JwtBearerDefaults", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication.OpenIdConnect", "OpenIdConnectDefaults", 0)]
    [InlineData("Microsoft.AspNetCore.Authentication.OpenIdConnect", "OpenIdConnectOptions", 0)]
    [InlineData("Microsoft.AspNetCore.Builder", "IApplicationBuilder", 0)]
    [InlineData("Microsoft.AspNetCore.Builder", "WebApplication", 0)]
    [InlineData("Microsoft.AspNetCore.Builder", "WebApplicationBuilder", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "CookieSecurePolicy", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "HttpContext", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "HttpMethods", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "HttpRequest", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "HttpResponse", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "SameSiteMode", 0)]
    [InlineData("Microsoft.AspNetCore.Http", "StatusCodes", 0)]
    [InlineData("Microsoft.Extensions.Configuration", "IConfiguration", 0)]
    [InlineData("Microsoft.Extensions.Configuration", "IConfigurationSection", 0)]
    [InlineData("Microsoft.Extensions.DependencyInjection", "IServiceCollection", 0)]
    [InlineData("Microsoft.Identity.Web", "MicrosoftIdentityWebAppAuthenticationBuilderExtensions", 0)]
    [InlineData("Microsoft.Identity.Web", "MicrosoftIdentityWebApiAuthenticationBuilderExtensions", 0)]
    [InlineData("System", "Exception", 0)]
    [InlineData("System", "Func", 1)]
    [InlineData("System", "StringComparer", 0)]
    [InlineData("System", "StringComparison", 0)]
    [InlineData("System", "Uri", 0)]
    [InlineData("System", "UriKind", 0)]
    [InlineData("System", "UriPartial", 0)]
    [InlineData("System.Threading.Tasks", "Task", 0)]
    public void should_catalog_the_type_in_both_projects(string @namespace, string name, int arity)
    {
        Assert.NotEmpty(_catalog.TypesNamed(@namespace, name, arity, GeneratedProjects.Application));
        Assert.NotEmpty(_catalog.TypesNamed(@namespace, name, arity, GeneratedProjects.Specifications));
    }
}
