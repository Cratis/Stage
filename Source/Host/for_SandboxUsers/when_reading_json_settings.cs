// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SandboxUsers;

public class when_reading_json_settings : Specification
{
    SandboxUsersOptions _options;

    void Because()
    {
        using var json = new MemoryStream(Encoding.UTF8.GetBytes("""
            { "Stage": { "Sandbox": { "Users": true, "UsersAllowRemote": true } } }
            """));
        var configuration = new ConfigurationBuilder().AddJsonStream(json).Build();
        _options = SandboxUsersOptions.Read(configuration, development: true);
    }

    [Fact] void should_enable_modeled_users() => _options.Enabled.ShouldBeTrue();
    [Fact] void should_allow_remote_selection() => _options.AllowRemote.ShouldBeTrue();
}
