// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelRuleAdmission;

public class when_admitting_modeled_rules : given.a_modeled_rule
{
    CommandDefinition _command = null!;
    ReadModelDefinition _readModel = null!;

    void Because()
    {
        Admit();
        var slices = _model.Collections.Single().Modules.Single().Features.Single().Slices;
        _command = slices.Single(slice => slice.Name == "Register").Command!;
        _readModel = slices.Single(slice => slice.Name == "List").ReadModel!;
    }

    [Fact] void should_keep_supported_command_validation_executable() => _refusal.ShouldBeNull();
    [Fact] void should_preserve_the_command_rule() => Assert.Single(_command.Rules).PropertyName.ShouldEqual("name");
    [Fact] void should_keep_queries_without_filters_executable() => Assert.Single(_readModel.Queries).Name.ShouldEqual("ListRecords");
}
