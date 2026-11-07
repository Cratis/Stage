// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Host.Published;
using Xunit;

namespace Cratis.Stage.Host.for_KernelConnectionGuard;

public class when_a_mounted_connection_implicitly_disables_authentication : Specification
{
    const string Connection = "chronicle://localhost:35000?disableTls=true";
    string _folder = null!;
    Exception? _error;
    AuthenticationMode _mode;

    void Establish()
    {
        _folder = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-mounted-connection")).FullName;
        File.WriteAllText(Path.Combine(_folder, "chronicle"), Connection + "\n");
    }

    void Because()
    {
        var mounted = SandboxRuntimeConventions.MountedConnectionString(name => name == SandboxRuntimeConventions.ConnectionStringFile ? Path.Combine(_folder, "chronicle") : null)!;
        _mode = mounted.AuthenticationMode;
        var options = new PublishedOptions("/package", "mongodb://storage", "TrustedProxy", ["127.0.0.1"], "app.example.com", null, "client", "synthetic");
        _error = Catch.Exception(() => KernelConnectionGuard.EnsureAllowed(mounted.ToString(), options));
    }

    void Destroy() => SpecTemporaryRoot.Delete(_folder);

    [Fact] void should_exercise_the_pinned_clients_implicit_unauthenticated_mode() => _mode.ShouldEqual(AuthenticationMode.None);
    [Fact] void should_refuse_the_mounted_connection_even_with_configured_credentials() => _error.ShouldBeOfExactType<PublishedModeRefused>();
    [Fact] void should_not_print_the_connection_string() => _error!.Message.ShouldNotContain(Connection);
}
