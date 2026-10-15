// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactImplementationGap;

public class when_refusing_malformed_contracts : given.a_gap
{
    Exception[] _failures = null!;
    void Because()
    {
        ImmutableArray<ArtifactImplementationGap>[] cases =
        [
            default, [null!], [_gap with { RequirementId = "" }], [_gap with { RequirementId = null! }],
            [_gap with { Owner = " " }], [_gap with { Origin = ArtifactImplementationGapOrigin.Unknown }],
            [_gap with { Role = (SemanticImplementationRole)999 }], [_gap with { Hints = default }],
            [_gap with { Hints = [null!] }], [_gap with { ContextVersion = 0 }], [_gap with { ResultVersion = 2 }],
            [_gap with { Stubs = [] }], [_gap with { Stubs = default }], [_gap with { Stubs = [null!] }],
            [_gap, _gap], [_gap with { Stubs = [_stub, _stub] }],
            WithStub(_stub with { RelativePath = "../other.cs" }), WithStub(_stub with { TypeName = "" }),
            WithStub(_stub with { MemberName = "" }), WithStub(_stub with { Signature = "signature\r\n" }),
            WithStub(_stub with { BodyLocator = null! }), WithStub(_stub with { BodyLocator = new(-1, 1) }),
            WithStub(_stub with { BodyLocator = new(1, 0) }), WithStub(_stub with { BodyLocator = new(int.MaxValue, 1) }),
            WithStub(_stub with { TypedContext = null! }), WithStub(_stub with { TypedContext = new("", []) }),
            WithStub(_stub with { TypedContext = new("Context", default) }),
            WithStub(_stub with { TypedContext = new("Context", [new("Name", "Type", true), new("Name", "Type", false)]) }),
            WithStub(_stub with { AcceptanceSpecifications = default }), WithStub(_stub with { AcceptanceSpecifications = [""] }),
            [_gap with { Stubs = [_stub, _stub with { MemberName = "Other", BodyLocator = new(121, 80) }] }],
            [_gap, _gap with { RequirementId = "other" }]
        ];
        _failures = [.. cases.Select(gaps => Catch.Exception(() => Digest(gaps)))];
    }

    [Fact] void should_refuse_all_32_malformed_payloads() => _failures.Length.ShouldEqual(32);
    [Fact] void should_report_contract_errors_for_every_payload() => _failures.All(failure => failure is InvalidArtifactRenderContract).ShouldBeTrue();
    ImmutableArray<ArtifactImplementationGap> WithStub(ArtifactImplementationStub stub) => [_gap with { Stubs = [stub] }];
}
