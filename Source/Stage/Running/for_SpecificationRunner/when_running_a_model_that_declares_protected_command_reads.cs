// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Running.for_SpecificationRunner;

#pragma warning disable CS0618 // The structural runner is the engine under test.
public class when_running_a_model_that_declares_protected_command_reads : Specification
{
    Exception? _protected;
    Exception? _unprotected;

    void Because()
    {
        var runner = new SpecificationRunner();
        _protected = Catch.Exception(() => runner.Run(new EventModel(Guid.NewGuid(), "Billing", []) { ProtectedCommandReads = true }));
        _unprotected = Catch.Exception(() => runner.Run(new EventModel(Guid.NewGuid(), "Billing", [])));
    }

    [Fact] void should_refuse_the_protected_model() => _protected.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_name_the_refusal_code() => _protected!.Message.ShouldContain("STAGE-ESM-044:");
    [Fact] void should_name_the_engine() => _protected!.Message.ShouldContain("structural engine");
    [Fact] void should_run_an_unprotected_model() => _unprotected.ShouldBeNull();
}
