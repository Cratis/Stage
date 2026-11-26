// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nominal_date_inputs;

public class with_a_protected_concept
{
    [Theory]
    [InlineData("pii", "PIIAttribute")]
    [InlineData("secret", "EncryptedAttribute")]
    void should_keep_nominal_compliance_and_exclude_causation_capture(string protection, string attribute)
    {
        var source = datetime_concept_model.Source().Replace("concept Moment : DateTime", $"concept Moment : DateTime {protection}", StringComparison.Ordinal);
        var assembly = datetime_concept_model.Load(source);
        var property = datetime_concept_model.Command(assembly).GetProperty("From");
        property.PropertyType.Name.ShouldEqual("Moment");
        property.GetCustomAttribute<ExplicitOffsetAttribute>().ShouldNotBeNull();
        property.GetCustomAttribute<NotAuditedAttribute>().ShouldNotBeNull();
        property.PropertyType.GetCustomAttributes().ShouldContain(value => value.GetType().Name == attribute);
    }
}
#endif
