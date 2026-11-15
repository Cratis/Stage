// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source.given;

public class a_subject : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticReadModel _subject = null!;
    protected SemanticCommand _command = null!;
    protected SemanticProperty _status = null!;

    void Establish()
    {
        _plan = a_lifecycle_source.Plan();
        _subject = _plan.ReadModels.Values.Single();
        _status = _subject.Properties.Single(property => property.Name == "status");
        _command = _plan.Commands.Values.Single(command => command.Name == "ChargeFee") with
        {
            Properties = [_subject.Properties.Single(property => property.IsIdentifier), _status]
        };
    }

    // Deliberately allow malformed definitions to exercise CommandSource's defensive ownership checks,
    // independently of the model validator's admission rules.
    protected void Models(params SemanticReadModel[] models) =>
        typeof(SemanticExecutionPlan).GetField("<ReadModels>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_plan, models.ToImmutableDictionary(model => model.Id));

    protected static SemanticId Id(int value) => SemanticId.Parse($"sem1:{value:x64}");

    protected ImmutableArray<SemanticPropertyValue> Status(SemanticValue value) => [new(_status.Id, value)];
}
