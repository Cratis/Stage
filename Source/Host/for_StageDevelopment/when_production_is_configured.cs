// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageDevelopment;

public class when_production_is_configured : Specification
{
    bool _development;
    bool _overridden;
    string? _environment;
    Exception? _refused;

    void Because()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production, ContentRootPath = Path.GetTempPath() });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [SandboxUsersOptions.Key] = "true" });
        _development = StageDevelopment.Decide(true, builder.Configuration);
        _environment = StageDevelopment.EnvironmentFor(_development, builder.Environment.EnvironmentName);
        _refused = Catch.Exception(() => SandboxIdentityOptions.Configure(builder.Services, builder.Configuration));
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { [StageDevelopment.Key] = "true" });
        _overridden = StageDevelopment.Decide(true, builder.Configuration);
        SandboxIdentityOptions.Configure(builder.Services, builder.Configuration);
    }

    [Fact] void should_not_treat_the_sandbox_as_development() => _development.ShouldBeFalse();
    [Fact] void should_leave_the_host_environment_alone() => _environment.ShouldBeNull();
    [Fact] void should_refuse_modeled_users_without_an_explicit_override() => _refused.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_allow_an_explicit_development_setting() => _overridden.ShouldBeTrue();
}
