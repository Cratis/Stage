// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_an_optional_identifier : a_command_only_plan
{
    Exception? _error;

    void Because()
    {
        var application = _originalModel.Application with
        {
            Modules = [.. _originalModel.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        ReadModels = [.. slice.ReadModels.Select(model => model with
                        {
                            Properties = [.. model.Properties.Select(property => property.IsIdentifier ? property with { Type = property.Type with { IsOptional = true } } : property)]
                        })]
                    })]
                })]
            })]
        };
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(_originalModel.LanguageVersion, _originalModel.SemanticVersion, application));
    }

    [Fact] void should_remain_an_invalid_contract() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_identify_the_required_scalar_identifier() => _error!.Message.ShouldContain("one required scalar identifier");
}
