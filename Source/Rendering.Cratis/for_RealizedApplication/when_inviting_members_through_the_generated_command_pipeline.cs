// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

/// <summary>
/// Dispatches tenant-bound invitations after the generated onboarding claims transformation.
/// </summary>
public partial class when_inviting_members_through_the_generated_command_pipeline : given.a_model_with_authentication_and_onboarding
{
    ProcessResult _tests = null!;
    string _application = null!;
    string _guards = null!;

    async Task Because()
    {
        _application = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-invitation-authorization")).FullName;
        foreach (var artifact in _plan.Artifacts)
        {
            var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, [.. artifact.Bytes]);
        }

        var probe = Path.Combine(_application, "OnboardingAuthorizationProbe");
        Directory.CreateDirectory(probe);
        await File.WriteAllTextAsync(Path.Combine(probe, "when_inviting_a_member.cs"), given.InvitationAuthorizationProbe.Source);
        await File.WriteAllTextAsync(Path.Combine(probe, "when_scoping_member_personas.cs"), given.TenantPersonaScopeProbe.Source);
        _guards = string.Join('\n', new[] { "GeneratedOnboarding/MemberPersonaClaims.cs", "GeneratedAuthentication/Personas.cs", "GeneratedOnboarding/OnboardingInvitations.cs" }
            .SelectMany(file => File.ReadAllLines(Path.Combine(_application, file)).Select((line, index) => $"{file}:{index + 1}: {line}")));
        var build = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(2), "dotnet", "build", $"{Application}.csproj", "-c", "Debug", "--nologo", "-nodeReuse:false", "-m:2");
        Assert.True(build.ExitCode == 0 && !build.Output.Contains("warning ", StringComparison.Ordinal), build.Output);
        _tests = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(2), "dotnet", "test", $"{Application}.csproj", "-c", "Debug", "--no-build", "--nologo", "--filter", "FullyQualifiedName~Lending.OnboardingAuthorizationProbe", "--logger", "console;verbosity=detailed");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_application);

    [Fact] void should_scope_personas_to_the_tenant_and_authorize_invitations() => Assert.True(_tests.ExitCode == 0 && PassingProbeSummary.IsMatch(_tests.Output), $"{_tests.Output}\n{_guards}");

    [GeneratedRegex(@"Total tests:\s+15\s+Passed:\s+15\s", RegexOptions.None, 1000)]
    private static partial Regex PassingProbeSummary { get; }
}
