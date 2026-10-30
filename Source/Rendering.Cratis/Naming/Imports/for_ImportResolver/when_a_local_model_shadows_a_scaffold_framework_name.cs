// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_a_local_model_shadows_a_scaffold_framework_name : Specification
{
    readonly TypeReference _model = new("Shop.Orders", "Subject", 1, ReferenceContext.Type);
    readonly TypeReference _framework = new("System.Reactive.Subjects", "Subject", 1, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(new("Shop.Orders", GeneratedProjects.Application, [new("Shop.Orders", "Subject", 1)], [_model, _framework]));

    [Fact] void should_use_the_same_namespace_model_binding() => _result.Spell(_model).ShouldEqual("Subject");
    [Fact] void should_not_mistake_global_namespace_membership_for_a_bare_binding() => _result.Spell(_framework).ShouldEqual("global::System.Reactive.Subjects.Subject");
    [Fact] void should_not_allocate_an_alias_for_the_scaffold_collision() => _result.Aliases.ShouldBeEmpty();
    [Fact] void should_not_add_an_import_that_cannot_outrank_the_local_model() => _result.Usings.ShouldBeEmpty();
}
