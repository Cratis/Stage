// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_building_code_provided_values : a_generated_application
{
    const string Body = "return context.Values.Dependency + \"|\" + context.Tenant.Value + \"|\" + context.Identity.Id + \"|\" + context.Identity.IsAuthenticated + \"|\" + context.Identity.ClaimValue(\"scope\") + \"|\" + string.Join(\",\", context.Identity.Roles) + \"|\" + context.Occurred.ToString(\"O\") + \"|\" + context.Arguments.Name;";

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = a_code_provided_model.Source(Body).Replace("module Fleet", "provide dependency String\n  ```csharp\n  return context.Arguments.Seed;\n  ```\nmodule Fleet", StringComparison.Ordinal);
        return a_code_provided_model.Plan(a_code_provided_model.RequestFromSource(source));
    }

    async Task Because()
    {
        AddGeneratedHostSupport("ProviderProbe.cs", """
            namespace Fleet;
            public static class ProviderProbe
            {
                public static int Calls;
                public sealed class Arguments(string seed, string name)
                {
                    public string Seed { get { Interlocked.Increment(ref Calls); return seed; } }
                    public string Name => name;
                }
            }
            """);
        AddGeneratedSpecification("when_reading_code_values.cs", Proof);
        await Run("debug-build.log", "build", "Fleet.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        await Run("code-provided-values", "test", "Fleet.csproj", "--configuration", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "console;verbosity=minimal");
        await Run("release-build.log", "build", "Fleet.csproj", "-c", "Release", "-warnaserror", "--nologo");
    }

    [Fact] void should_execute_the_generated_proof() => Assert.True(GeneratedFileExists("GeneratedProvides/ProvidedValues.cs"));

    const string Proof = """
        using System.Security.Claims;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Availability;
        using Cratis.Choreography.Availability.Audiences;
        using Cratis.Specifications;
        using Microsoft.Extensions.DependencyInjection;
        using NSubstitute;
        using Xunit;
        namespace Fleet;
        public class when_reading_code_values : Specification
        {
            [Fact]
            async Task should_share_dependencies_only_within_one_invocation_and_preserve_the_context()
            {
                ProviderProbe.Calls = 0;
                var principal = new CurrentPrincipalAccessor();
                using var identity = principal.BeginScope(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, "alice"), new Claim("scope", "invoices"), new Claim(ClaimTypes.Role, "editor") }, "proof")));
                var services = new ServiceCollection().AddSingleton<ICurrentPrincipalAccessor>(principal).BuildServiceProvider();
                var catalog = global::Fleet.GeneratedProvides.ProvidedValueResolvers.CreateCatalog(services);
                var subject = Substitute.For<IAvailabilitySubjectProvider>();
                subject.GetSubject().Returns(new AudienceSubject("tenant-one", "alice"));
                var audiences = Substitute.For<IAudienceResolver>();
                var request = new AvailabilityRequestContext(catalog, audiences, subject);
                var source = new ChoreographyContextValueSource(request, new ArcAvailabilityOptions());
                var received = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
                var first = new AvailabilityInvocation(typeof(when_reading_code_values), new ProviderProbe.Arguments("first", "command"), received);
                var result = await source.ReadForExecution(first, "region", default);
                result.Value.ShouldEqual("first|tenant-one|alice|True|invoices|editor|2026-11-23T10:00:00.0000000+00:00|command");
                (await source.ReadForExecution(first, "dependency", default)).Value.ShouldEqual("first");
                ProviderProbe.Calls.ShouldEqual(1);
                var second = new AvailabilityInvocation(typeof(when_reading_code_values), new Dictionary<string, object?> { ["Seed"] = "second", ["Name"] = "query" }, received.AddMinutes(1));
                ((string)(await source.ReadForExecution(second, "region", default)).Value!).StartsWith("second|tenant-one|alice|True|invoices|editor|").ShouldBeTrue();
                ProviderProbe.Calls.ShouldEqual(1);
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ReadForExecution(new AvailabilityInvocation(typeof(when_reading_code_values), first.Arguments, received), "region", new CancellationToken(true)));
            }
        }
        """;
}
#endif
