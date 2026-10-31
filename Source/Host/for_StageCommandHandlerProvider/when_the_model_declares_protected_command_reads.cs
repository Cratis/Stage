// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Runtime;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageCommandHandlerProvider;

public class when_the_model_declares_protected_command_reads : Specification
{
    Exception? _protected;
    Exception? _unprotected;

    void Because()
    {
        _protected = Catch.Exception(() => Create(new EventModel(Guid.NewGuid(), "Billing", []) { ProtectedCommandReads = true }));
        _unprotected = Catch.Exception(() => Create(new EventModel(Guid.NewGuid(), "Billing", [])));
    }

    [Fact] void should_refuse_the_protected_model() => _protected.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_name_the_refusal_code() => _protected!.Message.ShouldContain("STAGE-ESM-044:");
    [Fact] void should_serve_an_unprotected_model() => _unprotected.ShouldBeNull();

    static StageCommandHandlerProvider Create(EventModel model) => new(
        [model],
        [new DynamicTypeFactory()],
        [Substitute.For<IAppendProducedEvents>()],
        [Substitute.For<IProvideStageIdentity>()],
        [Substitute.For<ITenantIdAccessor>()]);
}
