// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Specifications;
using Cratis.Stage.Host.Published;
using Xunit;

namespace Cratis.Stage.Host.for_KernelConnectionGuard;

public class when_checking_a_connection
{
    [Fact]
    public void should_refuse_mounted_development_credentials_even_with_explicit_configuration()
    {
        var options = new PublishedOptions("/package", "mongodb://storage", "TrustedProxy", ["127.0.0.1"], "app.example.com", null, "client", "synthetic");
        var connection = new ChronicleConnectionStringBuilder("chronicle://remote:35000")
            .WithDevelopmentCredentials()
            .ToConnectionString();
        var error = Catch.Exception(() => KernelConnectionGuard.EnsureAllowed(connection.ToString(), options));
        error.ShouldBeOfExactType<PublishedModeRefused>();
        error!.Message.ShouldNotContain(connection.ToString());
    }

    [Theory]
    [InlineData("chronicle://localhost:35000", false, true)]
    [InlineData("chronicle://127.0.0.1:35000", false, true)]
    [InlineData("chronicle://[::1]:35000", false, true)]
    [InlineData("chronicle://remote:35000", false, false)]
    [InlineData("chronicle://remote:35000", true, false)]
    [InlineData("chronicle://client:synthetic@remote:35000", false, false)]
    [InlineData("chronicle://client:synthetic@remote:35000", true, true)]
    [InlineData("chronicle://localhost:35000?disableTls=true", false, false)]
    [InlineData("chronicle://127.0.0.1:35000?disableTls=true", true, false)]
    [InlineData("chronicle://[::1]:35000?disableTls=true", false, false)]
    [InlineData("chronicle://client:synthetic@localhost:35000?disableTls=true", false, true)]
    [InlineData("chronicle://client:synthetic@remote:35000?disableTls=true", true, true)]
    [InlineData("chronicle://localhost:35000?auth=none", false, false)]
    [InlineData("chronicle://client:synthetic@remote:35000?auth=none", true, false)]
    [InlineData("chronicle://localhost:35000?AUTH=NONE", true, false)]
    [InlineData("not a connection", true, false)]
    public void should_require_authentication_and_explicit_credentials_outside_loopback(string connection, bool credentials, bool allowed)
    {
        var options = new PublishedOptions("/package", "mongodb://storage", "TrustedProxy", ["127.0.0.1"], "app.example.com", null, credentials ? "client" : null, credentials ? "synthetic" : null);
        var error = Catch.Exception(() => KernelConnectionGuard.EnsureAllowed(connection, options));
        if (allowed)
        {
            error.ShouldBeNull();
        }
        else
        {
            error.ShouldBeOfExactType<PublishedModeRefused>();
            error!.Message.ShouldNotContain(connection);
        }
    }
}
