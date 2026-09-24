// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Types;
using NSubstitute;

namespace Cratis.Stage.Host.for_StageEndpointMapper.given;

/// <summary>
/// Registers the authorization services AddCratisArcCore registers, for fixtures that assemble Arc by hand.
/// </summary>
/// <remarks>
/// Arc's query authorization filter and pipeline resolve these from the request services. Without them every
/// query fails with a server error before its performer runs, which says nothing about Stage's routing.
/// </remarks>
public static class arc_authorization
{
    public static void Register(IServiceCollection services)
    {
        services.AddSingleton<CurrentPrincipalAccessor>();
        services.AddSingleton<ICurrentPrincipalAccessor>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<ICurrentPrincipalOverride>(provider => provider.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<IAuthorizationPolicyRuntime, ArcAuthorizationPolicyRuntime>();
        services.AddSingleton(Instances<IAnonymousEvaluator>(new AspNetAnonymousEvaluator(), new AnonymousEvaluator()));
        services.AddSingleton(Instances<IAuthorizationAttributeEvaluator>(new AspNetAuthorizationAttributeEvaluator(), new AuthorizationAttributeEvaluator()));
        services.AddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
        services.AddTransient<AuthorizationDeclarations>();
        services.AddTransient<AuthorizationEvaluation>();
    }

    static IInstancesOf<T> Instances<T>(params T[] instances)
        where T : class
    {
        var result = Substitute.For<IInstancesOf<T>>();
        result.GetEnumerator().Returns(_ => ((IEnumerable<T>)instances).GetEnumerator());
        return result;
    }
}
