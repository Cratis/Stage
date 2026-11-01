// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_model_type_collides_with_a_scaffold_global : Specification
{
    readonly TypeReference _model = new("Shop.Concepts", "Subject", 1, ReferenceContext.Type);
    readonly TypeReference _framework = new("System.Reactive.Subjects", "Subject", 1, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(
        new("Shop.Orders.Placing", GeneratedProjects.Application, [new("Shop.Concepts", "Subject", 1)], [_model, _framework]),
        FrameworkNameCatalog.Current);

    [Fact] void should_qualify_the_model_type() => _result.Spell(_model).ShouldEqual("global::Shop.Concepts.Subject");
    [Fact] void should_not_add_an_import() => _result.TopLevelUsings.ShouldBeEmpty();
    [Fact] void should_spell_the_framework_type_short() => _result.Spell(_framework).ShouldEqual("Subject");
}
