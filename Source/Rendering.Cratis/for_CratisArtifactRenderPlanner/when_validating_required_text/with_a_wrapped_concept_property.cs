// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Concepts;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using FluentValidation.Results;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_validating_required_text;

public class with_a_wrapped_concept_property : Specification
{
    Assembly _assembly = null!;
    Type _nameType = null!;
    ValidationResult[] _results = [];

    void Establish()
    {
        var model = invoice_model.Compile("""
            concept Name : String
            module Names
              feature Registration
                slice StateChange Register
                  command Register
                    personId Uuid identifier
                    name Name
                    validate
                      name not empty
                    produces Registered
                      for personId
                      name = name
                  event Registered
                    name Name
            """);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _assembly = opaque_policy_model.Compile(plan);
        _nameType = _assembly.GetTypes().Single(type => type.Name == "Register").GetProperty("Name")!.PropertyType;
    }

    void Because()
    {
        string?[] inputs = [null, string.Empty, " \t\r\n", "\u0085", "Ada", "\ufeff"];
        _results = [.. inputs.Select(input => validation_body_model.Validate(
            _assembly,
            "Register",
            Guid.Empty,
            input is null ? null! : Activator.CreateInstance(_nameType, input)!))];
    }

    [Fact] void should_validate_a_wrapped_text_concept_property() => typeof(ConceptAs<string>).IsAssignableFrom(_nameType).ShouldBeTrue();
    [Fact] void should_reject_a_null_concept() => _results[0].IsValid.ShouldBeFalse();
    [Fact] void should_reject_an_empty_concept_value() => _results[1].IsValid.ShouldBeFalse();
    [Fact] void should_reject_whitespace_in_a_concept_value() => _results[2].IsValid.ShouldBeFalse();
    [Fact] void should_reject_next_line_in_a_concept_value() => _results[3].IsValid.ShouldBeFalse();
    [Fact] void should_accept_nonblank_text_in_a_concept_value() => _results[4].IsValid.ShouldBeTrue();
    [Fact] void should_accept_a_byte_order_mark_like_the_canonical_source() => _results[5].IsValid.ShouldBeTrue();
}
#endif
