// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_RealizedApplication;

/// <summary>
/// Dispatches an invitation as a declared persona whose policy cannot be reduced to roles and claims.
/// </summary>
public partial class when_a_non_mappable_persona_invites_members_through_the_generated_command_pipeline : given.a_model_with_authentication_and_onboarding
{
    ProcessResult _tests = null!;
    string _application = null!;

    protected override string Declarations => """
        policy IsSteward
          require claim "owner" matches subject

        persona Steward
          description "Declared, but never matched to a caller"
          policy IsSteward


        """;

    protected override string Onboarding => DefaultOnboarding.Replace("by Librarian", "by Steward", StringComparison.Ordinal);

    async Task Because()
    {
        _application = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-nonmappable-inviter")).FullName;
        foreach (var artifact in _plan.Artifacts)
        {
            var path = Path.Combine(_application, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllBytesAsync(path, [.. artifact.Bytes]);
        }

        var probe = Path.Combine(_application, "OnboardingAuthorizationProbe");
        Directory.CreateDirectory(probe);
        await File.WriteAllTextAsync(Path.Combine(probe, "when_a_non_mappable_persona_invites.cs"), given.NonMappableInviterProbe.Source);
        var build = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(2), "dotnet", "build", $"{Application}.csproj", "-c", "Debug", "--nologo", "-nodeReuse:false", "-m:2");
        Assert.True(build.ExitCode == 0 && !build.Output.Contains("warning ", StringComparison.Ordinal), build.Output);
        _tests = await ApplicationProcess.Run(_application, TimeSpan.FromMinutes(2), "dotnet", "test", $"{Application}.csproj", "-c", "Debug", "--no-build", "--nologo", "--filter", "FullyQualifiedName~Lending.OnboardingAuthorizationProbe", "--logger", "console;verbosity=detailed");
    }

    void Destroy() => SpecTemporaryRoot.Delete(_application);

    [Fact] void should_not_let_a_declared_non_mappable_persona_invite_members() => Assert.True(_tests.ExitCode == 0 && PassingProbeSummary.IsMatch(_tests.Output), _tests.Output);

    [GeneratedRegex(@"Total tests:\s+4\s+Passed:\s+4\s", RegexOptions.None, 1000)]
    private static partial Regex PassingProbeSummary { get; }
}
