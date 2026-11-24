// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_building_optional_and_typed_code_provided_values : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan() => a_code_provided_model.Plan(
        a_code_provided_model.RequestFromSource(Declarations + "\n" + a_code_provided_model.Source("return \"unused\";")));

    async Task Because()
    {
        AddGeneratedHostSupport("Dependencies.props", $"""
            <Project>
              <ItemGroup Condition="'$(Configuration)' == 'Debug'">
                <PackageReference Include="Cratis.Arc.Availability" VersionOverride="{ScaffoldPackageVersions.ArcAvailability}" />
              </ItemGroup>
            </Project>
            """);
        AddGeneratedSpecification("when_reading_optional_and_typed_values.cs", Proof);
        await Run("optional-typed-debug-build.log", "build", "Fleet.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        await Run("optional-typed-debug-proof.log", "test", "Fleet.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--filter", "FullyQualifiedName~when_reading_optional_and_typed_values");
        await Run("optional-typed-release-build.log", "build", "Fleet.csproj", "-c", "Release", "-warnaserror", "--nologo");
    }

    [Fact] void should_build_and_execute_the_optional_and_typed_providers() => Assert.True(GeneratedFileExists("GeneratedProvides/ProvidedValues.cs"));

    const string Declarations = """
        provide missingText String optional
          ```csharp
          return null;
          ```
        provide presentText String optional
          ```csharp
          return "present";
          ```
        provide missingNumber Int optional
          ```csharp
          return null;
          ```
        provide presentNumber Int optional
          ```csharp
          return 14L;
          ```
        provide missingBoolean Bool optional
          ```csharp
          return null;
          ```
        provide presentBoolean Bool optional
          ```csharp
          return true;
          ```
        provide missingUuid Uuid optional
          ```csharp
          return null;
          ```
        provide presentUuid Uuid optional
          ```csharp
          return global::System.Guid.Empty;
          ```
        provide typedName String
          ```csharp
          return context.ArgumentsAs<global::Fleet.Fleet.Vessels.RegisterVessel.RegisterVessel>().Name;
          ```
        """;

    const string Proof = """
        using Cratis.Arc.Availability;
        using Cratis.Choreography.Availability.Audiences;
        using Cratis.Specifications;
        using Microsoft.Extensions.DependencyInjection;
        using NSubstitute;
        using Xunit;
        using RegisterVessel = Fleet.Fleet.Vessels.RegisterVessel.RegisterVessel;
        namespace Fleet;
        public class when_reading_optional_and_typed_values : Specification
        {
            [Fact]
            async Task should_resolve_optional_results_and_cast_the_real_bound_command()
            {
                var collection = new ServiceCollection();
                global::Fleet.GeneratedProvides.Registration.Register(collection);
                using var services = collection.BuildServiceProvider();
                var catalog = global::Fleet.GeneratedProvides.ProvidedValueResolvers.CreateCatalog(services);
                var subject = Substitute.For<IAvailabilitySubjectProvider>();
                subject.GetSubject().Returns(new AudienceSubject("tenant", "alice"));
                var context = new AvailabilityRequestContext(catalog, Substitute.For<IAudienceResolver>(), subject);
                var source = new ChoreographyContextValueSource(context, new ArcAvailabilityOptions());
                var command = new RegisterVessel(Name: "Typed Orion", VesselId: Guid.Empty);
                var invocation = new AvailabilityInvocation(typeof(RegisterVessel), command, new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
                foreach (var name in new[] { "missingText", "missingNumber", "missingBoolean", "missingUuid" })
                {
                    (await source.ReadForExecution(invocation, name, default)).IsPresent.ShouldBeFalse();
                }
                (await source.ReadForExecution(invocation, "presentText", default)).Value.ShouldEqual("present");
                (await source.ReadForExecution(invocation, "presentNumber", default)).Value.ShouldEqual(14L);
                (await source.ReadForExecution(invocation, "presentBoolean", default)).Value.ShouldEqual(true);
                (await source.ReadForExecution(invocation, "presentUuid", default)).Value.ShouldEqual(Guid.Empty);
                (await source.ReadForExecution(invocation, "typedName", default)).Value.ShouldEqual("Typed Orion");
            }
        }
        """;
}
#endif
