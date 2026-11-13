// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Xml.Linq;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_building_provided_values(when_building_provided_values.context fixture) : IClassFixture<when_building_provided_values.context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_the_five_generated_specifications() => fixture.Results.Length.ShouldEqual(5);
    [Fact] void should_pass_every_generated_specification() => fixture.Results.Select(result => result.Outcome).Distinct().ShouldContainOnly(["Passed"]);

    public class context : a_generated_application
    {
        public string DebugWarnings { get; private set; } = null!;
        public string ReleaseWarnings { get; private set; } = null!;
        public ImmutableArray<GeneratedTestResult> Results { get; private set; }

        protected override ArtifactRenderPlan CreatePlan() => a_fleet_model.Plan(a_fleet_model.Source);

        async Task Because()
        {
            AddGeneratedSpecification("when_resolving_provided_values.cs", """
                #if DEBUG
                using Cratis.Choreography.Availability.Context.Providers;
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.ReadModels;
                using Cratis.Specifications;
                using Microsoft.Extensions.DependencyInjection;
                using NSubstitute;
                using Xunit;

                namespace Fleet;

                public class when_resolving_provided_values : Specification
                {
                    object? _region;
                    object? _operator;
                    object? _vessel;
                    object? _providedVessel;
                    object? _unknownVessel;

                    async Task Because()
                    {
                        var vesselId = Guid.NewGuid();
                        var known = new global::Fleet.Fleet.Vessels.Vessels.Vessel("Aurora", vesselId);
                        var readModels = Substitute.For<IReadModels>();
                        readModels.GetInstanceById<global::Fleet.Fleet.Vessels.Vessels.Vessel>(Arg.Any<ReadModelKey>()).Returns(Task.FromResult(known));
                        var noReadModels = Substitute.For<IReadModels>();
                        noReadModels.GetInstanceById<global::Fleet.Fleet.Vessels.Vessels.Vessel>(Arg.Any<ReadModelKey>()).Returns(Task.FromResult<global::Fleet.Fleet.Vessels.Vessels.Vessel>(null!));
                        var empty = new ServiceCollection();
                        empty.AddSingleton(noReadModels);
                        GeneratedProvides.Registration.Register(empty);
                        using var emptyProvider = empty.BuildServiceProvider();
                        var services = new ServiceCollection();
                        services.AddSingleton(readModels);
                        GeneratedProvides.Registration.Register(services);
                        using var provider = services.BuildServiceProvider();
                        ((InMemoryHostContextValues)provider.GetRequiredService<IHostContextValues>()).Set("operator", "Ada");
                        var resolver = provider.GetRequiredService<GeneratedProvides.ProvidedValueResolvers>()
                            .Create(["Fleet", "Vessels", "RegisterVessel"], new Dictionary<string, object?>());
                        _region = (await resolver.Resolve("$context.region")).Value;
                        _operator = (await resolver.Resolve("$context.operator")).Value;
                        _vessel = (await resolver.Resolve("$context.vessel")).Value;
                        var reading = provider.GetRequiredService<GeneratedProvides.ProvidedValueResolvers>()
                            .Create(["Fleet", "Vessels", "Vessels"], new Dictionary<string, object?> { ["arguments"] = new Dictionary<string, object?> { ["vesselId"] = vesselId } });
                        _providedVessel = (await reading.Resolve("$context.vessel")).Value;
                        var unknown = emptyProvider.GetRequiredService<GeneratedProvides.ProvidedValueResolvers>()
                            .Create(["Fleet", "Vessels", "Vessels"], new Dictionary<string, object?> { ["arguments"] = new Dictionary<string, object?> { ["vesselId"] = Guid.NewGuid() } });
                        _unknownVessel = (await unknown.Resolve("$context.vessel")).Value;
                    }

                    [Fact] void should_resolve_the_literal() => Assert.Equal("Nordics", _region);
                    [Fact] void should_resolve_the_host_value() => Assert.Equal("Ada", _operator);
                    [Fact] void should_read_the_vessel_through_its_query() => Assert.Equal("Aurora", ((global::Fleet.Fleet.Vessels.Vessels.Vessel)_providedVessel!).Name);
                    [Fact] void should_leave_an_unknown_vessel_absent() => Assert.Null(_unknownVessel);
                    [Fact] void should_not_see_a_value_declared_by_a_sibling_slice() => Assert.Null(_vessel);
                }
                #endif
                """);
            try
            {
                var warnings = await Run("debug-build.log", "build", "Fleet.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "-m:3", "--nologo");
                DebugWarnings = BuildWarnings(warnings);
                await Run("debug-test.log", "test", "Fleet.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo", "--logger", "trx;LogFileName=generated.trx", "--results-directory", _evidence.FullName);
                ReleaseWarnings = BuildWarnings(await Run("release-build.log", "build", "Fleet.csproj", "-c", "Release", "-t:Rebuild", "-warnaserror", "-m:3", "--nologo"));
                Results = GeneratedTestResults.Read(XDocument.Load(Path.Combine(_evidence.FullName, "generated.trx")));
            }
            finally
            {
                Cleanup();
            }
        }
    }
}
#endif
