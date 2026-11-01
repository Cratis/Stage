// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UserInterfaceDomainsInput;

public class when_creating_the_input : Specification
{
    ArtifactRenderInput _input = null!;
    ArtifactRenderInput _reordered = null!;
    bool _read;
    ImmutableDictionary<string, ImmutableArray<string>>? _domains;

    void Because()
    {
        _input = UserInterfaceDomainsInput.Create([Domain("support", "d", "c", "d"), Domain("sales/retail", "b", "a")]);
        _reordered = UserInterfaceDomainsInput.Create([Domain("Sales/Retail", "a", "b"), Domain("Support", "c", "d")]);
        _read = UserInterfaceDomainsInput.TryRead(ArtifactRenderProfile.Create("cratis", "1", "stage", "1", [_input]), out _domains);
    }

    [Fact] void should_use_the_stable_name() => _input.Name.ShouldEqual("cratis-render:user-interface-domains");
    [Fact] void should_use_the_first_version() => _input.Version.ShouldEqual("1");
    [Fact] void should_write_normalized_domains_with_distinct_identities_in_ordinal_order() => System.Text.Encoding.UTF8.GetString(_input.Bytes.AsSpan()).ShouldEqual("{\"Sales/Retail\":[\"a\",\"b\"],\"Support\":[\"c\",\"d\"]}");
    [Fact] void should_not_depend_on_the_order_or_spelling_given() => _input.Bytes.SequenceEqual(_reordered.Bytes).ShouldBeTrue();
    [Fact] void should_read_back() => _read.ShouldBeTrue();
    [Fact] void should_read_the_identities_by_domain() => string.Join(',', _domains!["Support"]).ShouldEqual("c,d");

    static KeyValuePair<string, IEnumerable<string>> Domain(string domain, params string[] slices) => KeyValuePair.Create(domain, (IEnumerable<string>)slices);
}
