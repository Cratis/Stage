// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageRuntimeRegistrar;

public class when_registering_unenforced_rules : Specification
{
    EventModel _model = null!;
    IServiceProvider _services = null!;
    Exception? _registrationException;
    Exception? _handlerException;

    void Establish()
    {
        _model = EventModelLoader.LoadFromSource("""
            concept Name : String
              validate
                not empty
            module Registry
              feature Records
                slice StateChange Register
                  command RegisterRecord
                    name Name
                    produces RecordRegistered
                      name = name
                  event RecordRegistered
                    name Name
            """);
        _services = Substitute.For<IServiceProvider>();
    }

    async Task Because()
    {
        _registrationException = await Catch.Exception(() =>
            StageRuntimeRegistrar.RegisterAsync(_services, "registry", _model, NullLogger.Instance));
        _handlerException = Catch.Exception(() => _ = new StageCommandHandlerProvider([_model], [], [], []));
    }

    [Fact] void should_fault_registration() => _registrationException.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_refuse_handlers_even_without_dependencies() => _handlerException.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_not_resolve_a_client() => _services.DidNotReceive().GetService(Arg.Any<Type>());
    [Fact] void should_report_the_runtime_rule() => _registrationException!.Message.ShouldContain("STAGE-ESM-081:");
    [Fact] void should_report_the_handler_rule() => _handlerException!.Message.ShouldContain("STAGE-ESM-081:");
}
#endif
