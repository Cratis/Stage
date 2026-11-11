// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Queries;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_StatePolicies;

// A list narrowed by a policy renders as an Arc row narrowing: only the vessels whose recorded plan is enabled are kept, and a vessel
// with no plan, an unreadable plan or a disabled plan is dropped, never returned.
public class when_rendering_a_query_that_narrows_its_rows : Specification
{
    const string Source = """
        concept VesselId : String
        policy OnThePlan
          reads VesselPlan as plan by vesselId
          require plan.enabled == true
        module Fleet
          feature Vessels
            slice StateView VesselList
              readmodel Vessel
                vesselId VesselId key
                name String
              readmodel VesselPlan
                vesselId VesselId key
                enabled Bool
              event VesselNamed
                name String
              event VesselPlanSet
                enabled Bool
              projection BuildVessel => Vessel
                from VesselNamed
                  vesselId = $eventSourceId
                  name = name
              projection BuildVesselPlan => VesselPlan
                from VesselPlanSet
                  vesselId = $eventSourceId
                  enabled = enabled
              query VesselsOnThePlan => Vessel[]
                narrow OnThePlan
        """;

    ArtifactRenderPlan _plan = null!;
    List<string> _kept = [];
    int _total;
    List<string> _firstPage = [];

    void Because()
    {
        _plan = opaque_policy_model.Plan(Source);
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        var assembly = opaque_policy_model.Compile(_plan);
        var vessel = assembly.GetTypes().Single(type => type.Name == "Vessel");
        var plan = assembly.GetTypes().Single(type => type.Name == "VesselPlan");
        var id = assembly.GetTypes().Single(type => type.Name == "VesselId");
        var narrowingType = assembly.GetTypes().Single(type => type.Name == "VesselsOnThePlanNarrowing");
        object Id(string value) => Activator.CreateInstance(id, value)!;
        object Build(Type type, string value, object other) => type.GetConstructors().Single().Invoke([.. type.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType == id ? Id(value) : other)]);
        object Vessel(string value) => Build(vessel, value, value.ToUpperInvariant());
        object Plan(string value, bool enabled) => Build(plan, value, enabled);
        var vessels = new[] { "v1", "v2", "v3", "v4", "v5" }.Select(Vessel).ToList();
        var plans = new Dictionary<string, object> { ["v1"] = Plan("v1", true), ["v2"] = Plan("v2", false), ["v3"] = Plan("v3", true), ["v5"] = Plan("v5", true) };
        var readModels = ReadModelsStandIn.Create(vessel, vessels, plan, plans, unreadable: "v5");
        var narrowing = (IQueryRowNarrowing)Activator.CreateInstance(narrowingType, readModels)!;
        var context = new QueryContext("Fleet.Vessels.VesselsOnThePlan", CorrelationId.New(), new Paging(0, 1, true), Sorting.None);
        var predicate = narrowing.GetPredicate(vessel, context).AsTask().GetAwaiter().GetResult();
        Assert.NotNull(predicate);
        var narrowed = typeof(IQueryRowNarrowing).Assembly.GetType("Cratis.Arc.Queries.QueryRowNarrower")!.GetMethod("Narrow", BindingFlags.NonPublic | BindingFlags.Static)!;
        var queryable = (IQueryable)typeof(Queryable).GetMethods().Single(method => method.Name == nameof(Queryable.AsQueryable) && method.IsGenericMethod).MakeGenericMethod(vessel).Invoke(null, [CastList(vessels, vessel)])!;
        var task = (Task<object>)narrowed.Invoke(null, [context.Name, vessel, queryable, context, new SingleService(narrowing)])!;
        var narrowedResult = task.GetAwaiter().GetResult();
        var rendered = new QueryableQueryRenderer().Execute((IQueryable)narrowedResult, context);
        _total = rendered.TotalItems;
        _firstPage = [.. ((System.Collections.IEnumerable)rendered.Data).Cast<object>().Select(row => (string)row.GetType().GetProperty("Name")!.GetValue(row)!)];
        var all = new QueryableQueryRenderer().Execute((IQueryable)narrowedResult, context with { Paging = Paging.NotPaged });
        _kept = [.. ((System.Collections.IEnumerable)all.Data).Cast<object>().Select(row => (string)row.GetType().GetProperty("Name")!.GetValue(row)!)];
    }

    [Fact] void should_keep_only_the_vessels_whose_plan_is_enabled() => _kept.ShouldEqual(["V1", "V3"]);
    [Fact] void should_drop_a_vessel_with_a_disabled_plan() => _kept.ShouldNotContain("V2");
    [Fact] void should_drop_a_vessel_with_no_plan() => _kept.ShouldNotContain("V4");
    [Fact] void should_drop_a_vessel_whose_plan_cannot_be_read() => _kept.ShouldNotContain("V5");
    [Fact] void should_count_only_the_allowed_rows() => _total.ShouldEqual(2);
    [Fact] void should_page_the_allowed_rows() => _firstPage.ShouldEqual(["V1"]);

    static object CastList(List<object> items, Type element)
    {
        var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
        foreach (var item in items) list.Add(item);

        return list;
    }

    sealed class SingleService(IQueryRowNarrowing narrowing) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(IEnumerable<IQueryRowNarrowing>) ? new[] { narrowing } : null;
    }

    public class ReadModelsStandIn : DispatchProxy
    {
        Type _vessel = null!;
        List<object> _vessels = [];
        Type _plan = null!;
        Dictionary<string, object> _plans = [];
        string _unreadable = string.Empty;

        public static IReadModels Create(Type vessel, List<object> vessels, Type plan, Dictionary<string, object> plans, string unreadable)
        {
            var proxy = Create<IReadModels, ReadModelsStandIn>();
            var self = (ReadModelsStandIn)(object)proxy;
            self._vessel = vessel;
            self._vessels = vessels;
            self._plan = plan;
            self._plans = plans;
            self._unreadable = unreadable;

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var result = targetMethod!.GetGenericArguments()[0];
            if (targetMethod.Name == nameof(IReadModels.GetInstances))
            {
                var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(result))!;
                foreach (var vessel in _vessels) list.Add(vessel);

                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(IEnumerable<>).MakeGenericType(result)).Invoke(null, [list]);
            }

            var key = args![0]!.ToString()!;
            var wanted = key.ToLowerInvariant();
            if (wanted == _unreadable)
            {
                return typeof(ReadModelsStandIn).GetMethod(nameof(Failed), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(result).Invoke(null, null);
            }

            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(result).Invoke(null, [_plans.GetValueOrDefault(wanted)]);
        }

        static Task<T> Failed<T>() => Task.FromException<T>(new InvalidOperationException("The read model store is unavailable."));
    }
}
#endif
