// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class a_published_configuration : Specification
{
    protected Dictionary<string, string?> _values = null!;

    private protected PublishedOptions Options() => PublishedOptions.From(new ConfigurationBuilder().AddInMemoryCollection(_values).Build());

    void Establish() => _values = new()
    {
        ["Stage:Published:Storage:ConnectionString"] = "mongodb://mongo.internal:27017",
        ["Stage:Published:Authentication:Mode"] = "TrustedProxy",
        ["Stage:Published:TrustedProxies:0"] = "127.0.0.1",
        ["Stage:Published:TrustedProxies:1"] = "::1",
        ["AllowedHosts"] = "my-app.cratis.app"
    };
}
