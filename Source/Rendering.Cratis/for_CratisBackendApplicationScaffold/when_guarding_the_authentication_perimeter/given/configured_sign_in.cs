// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Specifications;
using Microsoft.Extensions.Configuration;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.when_guarding_the_authentication_perimeter.given;

public class configured_sign_in : Specification
{
    protected Dictionary<string, string?> _values = null!;
    protected object? _settings;
    protected Exception? _error;

    void Establish() => _values = new()
    {
        [$"{the_emitted_perimeter.EntraIdKey}:ClientId"] = "<synthetic-client-id>",
        [$"{the_emitted_perimeter.EntraIdKey}:TenantId"] = "<synthetic-tenant-id>",
        [$"{the_emitted_perimeter.EntraIdKey}:ClientCredentials:0:SourceType"] = "ClientSecret",
        [$"{the_emitted_perimeter.EntraIdKey}:ClientCredentials:0:ClientSecret"] = "<synthetic-client-secret>",
        [the_emitted_perimeter.PublicOriginKey] = the_emitted_perimeter.PublicOrigin
    };

    protected void Read()
    {
        try
        {
            _settings = the_emitted_perimeter.Read(new ConfigurationBuilder().AddInMemoryCollection(_values).Build());
        }
        catch (TargetInvocationException exception)
        {
            _error = exception.InnerException;
        }
    }

    protected bool NamesOnly(string key) => _error?.GetType().Name == "InvalidAuthenticationConfiguration" &&
        _error.Message.Contains(key, StringComparison.Ordinal) &&
        !_values.Values.Any(value => !string.IsNullOrEmpty(value) && _error.Message.Contains(value, StringComparison.Ordinal));

    protected Uri Origin => (Uri)_settings!.GetType().GetProperty("PublicOrigin")!.GetValue(_settings)!;
}
