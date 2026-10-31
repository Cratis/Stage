// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.ReadModels;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_streaming_performed_queries;

public class a_query_discovered_by_arc : given.a_live_query
{
    ModelBoundQueryPerformer _performer = null!;
    object _result = null!;

    async Task Because()
    {
        await Prepare();
        await using var provider = new ServiceCollection()
            .AddSingleton((IReadModels)_dependencies[0])
            .AddSingleton((IQueryContextManager)_dependencies[1])
            .AddSingleton((ITenantIdAccessor)_dependencies[2])
            .AddSingleton((ICurrentPrincipalAccessor)_dependencies[3])
            .BuildServiceProvider();
        _performer = new(_method.DeclaringType, _method.DeclaringType.FullName, _method, provider.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        _result = await _performer.Perform(QueryContext.NotSet with { Dependencies = _dependencies, Arguments = new() { ["name"] = "Ada" } });
    }

    [Fact] void should_return_the_subject_for_observable_delivery() => _method.ReturnType.IsInstanceOfType(_result).ShouldBeTrue();
    [Fact] void should_not_offer_snapshot_paging() => _performer.SupportsPaging.ShouldBeFalse();
    [Fact] void should_keep_the_filter_as_the_only_query_argument() => _performer.Parameters.Select(parameter => parameter.Name).ShouldEqual(["name"]);
    [Fact] void should_inject_all_four_context_services() => _performer.Dependencies.Count().ShouldEqual(4);
}
#endif
