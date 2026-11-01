// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

internal static class BodyCacheAccessPolicy
{
    internal static bool IsProtected(IEnumerable<BodyCacheAccessRule> rules, bool cache) => !rules.Any(rule => UntrustedWrite(rule, cache));

    internal static bool TrustedOwner(BodyCachePrincipal principal) => principal is BodyCachePrincipal.CurrentUser or BodyCachePrincipal.System or BodyCachePrincipal.Administrators;

    static bool UntrustedWrite(BodyCacheAccessRule rule, bool cache)
    {
        if (rule.InheritOnly) return false;
        if (rule.Principal is BodyCachePrincipal.CreatorOwner or BodyCachePrincipal.CreatorGroup) return false;
        if (TrustedOwner(rule.Principal) || !rule.Allow) return false;
        var mutation = BodyCacheRights.ReplaceChildren | BodyCacheRights.Control;
        if (cache) mutation |= BodyCacheRights.Write | BodyCacheRights.Delete;
        return (rule.Rights & mutation) != default;
    }
}
