// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using FluentValidation.Results;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_validating_required_text;

public class with_a_primitive_property : Specification
{
    Assembly _assembly = null!;
    ValidationResult _result = null!;

    void Establish()
    {
        var model = invoice_model.Compile("""
            module Names
              feature Registration
                slice StateChange Register
                  command Register
                    personId Uuid identifier
                    name String
                    validate
                      name not empty
                    produces Registered
                      for personId
                      name = name
                  event Registered
                    name String
            """);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
    }

    void Because() => _result = validation_body_model.Validate(_assembly, "Register", Guid.Empty, string.Empty);

    [Fact] void should_reject_an_empty_required_text() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_builtin_error_code() => _result.Errors.Single().ErrorCode.ShouldEqual("NotEmptyValidator");
    [Fact] void should_preserve_the_rejection_message() => _result.Errors.Single().ErrorMessage.ShouldEqual("A required value is empty.");
}
#endif
