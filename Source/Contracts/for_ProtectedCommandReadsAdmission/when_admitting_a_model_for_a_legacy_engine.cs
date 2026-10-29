// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProtectedCommandReadsAdmission;

public class when_admitting_a_model_for_a_legacy_engine : Specification
{
    Exception? _protected;
    Exception? _unprotected;

    void Because()
    {
        _protected = Catch.Exception(() => ProtectedCommandReadsAdmission.EnsureExecutableBy(new EventModel(Guid.NewGuid(), "Billing", []) { ProtectedCommandReads = true }, "billing.play", "eventmodel"));
        _unprotected = Catch.Exception(() => ProtectedCommandReadsAdmission.EnsureExecutableBy(new EventModel(Guid.NewGuid(), "Billing", []), "billing.play", "eventmodel"));
    }

    [Fact] void should_refuse_a_protected_model() => _protected.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_name_the_refusal_code() => _protected!.Message.ShouldContain("STAGE-ESM-044:");
    [Fact] void should_name_the_declaration() => _protected!.Message.ShouldContain("command reads protected");
    [Fact] void should_name_the_engine() => _protected!.Message.ShouldContain("eventmodel engine");
    [Fact] void should_admit_an_unprotected_model() => _unprotected.ShouldBeNull();
}
