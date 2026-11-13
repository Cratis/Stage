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

public class when_building_availability(when_building_availability.context fixture) : IClassFixture<when_building_availability.context>
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

        protected override ArtifactRenderPlan CreatePlan() => a_fleet_model.Plan(an_available_fleet_model.Source);

        async Task Because()
        {
            AddGeneratedSpecification("when_gating_parts_on_audiences.cs", """
                #if DEBUG
                using Cratis.Arc.Availability;
                using Cratis.Choreography.Availability;
                using Cratis.Choreography.Availability.Audiences;
                using Cratis.Choreography.Availability.Context;
                using Cratis.Specifications;
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;
                using Xunit;

                namespace Fleet;

                public class when_gating_parts_on_audiences : Specification
                {
                    object? _plusDataQuality;
                    object? _otherDataQuality;
                    object? _betaCommand;
                    object? _withdrawn;
                    object? _scope;

                    async Task Because()
                    {
                        var source = new InMemoryAudienceSource();
                        await source.Grant(AudienceLevel.Tenant, "plus-tenant", "Plus");
                        await source.Grant(AudienceLevel.Tenant, "beta-tenant", "Beta");
                        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
                        builder.Services.AddSingleton<IAudienceSource>(source);
                        global::Fleet.GeneratedProvides.Registration.Register(builder.Services);
                        global::Fleet.GeneratedAvailability.Registration.Register(builder);
                        using var provider = builder.Services.BuildServiceProvider();

                        var catalog = provider.GetRequiredService<AvailabilityCatalog>();
                        var values = provider.GetRequiredService<ContextValueCatalog>();
                        var audiences = provider.GetRequiredService<IAudienceResolver>();
                        var evaluator = new AvailabilityEvaluator(catalog);

                        async Task<object?> Evaluate(AvailabilityExpression gate, string tenant)
                        {
                            var resolver = values.CreateResolver(
                                ContextScope.Of("Fleet", "Vessels", "Vessels"),
                                null,
                                async cancellationToken => await audiences.Resolve(new AudienceSubject(tenant, null), cancellationToken));
                            return (await evaluator.Evaluate(gate, resolver)).Outcome;
                        }

                        var dataQuality = AvailabilityExpression.With("DataQuality");
                        _plusDataQuality = await Evaluate(dataQuality, "plus-tenant");
                        _otherDataQuality = await Evaluate(dataQuality, "beta-tenant");
                        _betaCommand = await Evaluate(AvailabilityExpression.AnyOf(AvailabilityExpression.For("Plus"), AvailabilityExpression.For("Beta")), "beta-tenant");
                        await source.Withdraw(AudienceLevel.Tenant, "plus-tenant", "Plus");
                        _withdrawn = await Evaluate(dataQuality, "plus-tenant");
                        _scope = string.Join("/", provider.GetRequiredService<ArcAvailabilityOptions>().ScopeFor(typeof(global::Fleet.Fleet.Vessels.RegisterVessel.RegisterVessel)).Path);
                    }

                    [Fact] void should_open_a_block_for_a_tenant_in_its_audience_once_the_provided_value_matches() => Assert.Equal(AvailabilityOutcome.Available, _plusDataQuality);
                    [Fact] void should_keep_a_block_closed_for_a_tenant_outside_its_audience() => Assert.Equal(AvailabilityOutcome.Unavailable, _otherDataQuality);
                    [Fact] void should_open_a_command_for_either_audience() => Assert.Equal(AvailabilityOutcome.Available, _betaCommand);
                    [Fact] void should_close_a_block_once_the_audience_is_withdrawn() => Assert.Equal(AvailabilityOutcome.Unavailable, _withdrawn);
                    [Fact] void should_scope_a_part_by_its_slice() => Assert.Equal("Fleet/Vessels/RegisterVessel", _scope);
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
