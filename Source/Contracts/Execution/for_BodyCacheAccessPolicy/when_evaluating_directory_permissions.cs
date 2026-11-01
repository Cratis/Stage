// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyCacheAccessPolicy;

public class when_evaluating_directory_permissions : Specification
{
    [Theory]
    [InlineData(BodyCachePrincipal.CreatorOwner)]
    [InlineData(BodyCachePrincipal.CreatorGroup)]
    void should_accept_inherit_only_creator_templates_on_an_ancestor(BodyCachePrincipal principal)
    {
        var template = new BodyCacheAccessRule(principal, BodyCacheRights.All, Allow: true, InheritOnly: true);
        BodyCacheAccessPolicy.IsProtected([template], cache: false).ShouldBeTrue();
    }

    [Theory]
    [InlineData(BodyCachePrincipal.CreatorOwner)]
    [InlineData(BodyCachePrincipal.CreatorGroup)]
    void should_not_treat_creator_placeholders_as_principals_on_the_directory_itself(BodyCachePrincipal principal)
    {
        var placeholder = new BodyCacheAccessRule(principal, BodyCacheRights.All, Allow: true, InheritOnly: false);
        BodyCacheAccessPolicy.IsProtected([placeholder], cache: true).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_skip_an_inherit_only_non_owner_rule_when_checking_the_ancestor(bool inherited)
    {
        var template = new BodyCacheAccessRule(BodyCachePrincipal.Other, BodyCacheRights.All, Allow: true, InheritOnly: true, IsInherited: inherited);
        BodyCacheAccessPolicy.IsProtected([template], cache: false).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_effective_non_owner_write_on_the_cache_even_when_inherited(bool inherited)
    {
        var grant = new BodyCacheAccessRule(BodyCachePrincipal.Other, BodyCacheRights.Write, Allow: true, InheritOnly: false, IsInherited: inherited);
        BodyCacheAccessPolicy.IsProtected([grant], cache: true).ShouldBeFalse();
    }

    [Fact]
    void should_not_let_a_safe_parent_template_hide_an_effective_inherited_grant_on_the_cache()
    {
        var template = new BodyCacheAccessRule(BodyCachePrincipal.CreatorOwner, BodyCacheRights.All, Allow: true, InheritOnly: true);
        var effective = new BodyCacheAccessRule(BodyCachePrincipal.Other, BodyCacheRights.Write, Allow: true, InheritOnly: false, IsInherited: true);
        BodyCacheAccessPolicy.IsProtected([template], cache: false).ShouldBeTrue();
        BodyCacheAccessPolicy.IsProtected([template, effective], cache: true).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_an_effective_parent_grant_that_can_replace_its_children(bool inherited)
    {
        var grant = new BodyCacheAccessRule(BodyCachePrincipal.Other, BodyCacheRights.ReplaceChildren, Allow: true, InheritOnly: false, IsInherited: inherited);
        BodyCacheAccessPolicy.IsProtected([grant], cache: false).ShouldBeFalse();
    }

    [Fact]
    void should_not_mistake_a_deny_rule_for_a_write_grant()
    {
        var denied = new BodyCacheAccessRule(BodyCachePrincipal.Other, BodyCacheRights.All, Allow: false, InheritOnly: false, IsInherited: true);
        BodyCacheAccessPolicy.IsProtected([denied], cache: true).ShouldBeTrue();
    }

    [Theory]
    [InlineData(BodyCachePrincipal.CurrentUser)]
    [InlineData(BodyCachePrincipal.System)]
    [InlineData(BodyCachePrincipal.Administrators)]
    void should_accept_effective_inherited_access_for_a_trusted_principal(BodyCachePrincipal principal)
    {
        var grant = new BodyCacheAccessRule(principal, BodyCacheRights.All, Allow: true, InheritOnly: false, IsInherited: true);
        BodyCacheAccessPolicy.IsProtected([grant], cache: true).ShouldBeTrue();
    }
}
