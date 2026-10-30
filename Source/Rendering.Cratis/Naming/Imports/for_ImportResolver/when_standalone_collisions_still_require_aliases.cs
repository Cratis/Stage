// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_standalone_collisions_still_require_aliases : Specification
{
    readonly TypeReference _model = new("Shop.Concepts", "Subject", 1, ReferenceContext.Type);
    readonly TypeReference _framework = new("System.Reactive.Subjects", "Subject", 1, ReferenceContext.Type);
    ImportResolution _result = null!;

    void Because() => _result = ImportResolver.Resolve(new("Shop.Orders.Placing", GeneratedProjects.Application, [new("Shop.Concepts", "Subject", 1)], [_model, _framework]) { HasScaffoldGlobalUsings = false });

    [Fact] void should_keep_the_standalone_model_spelling() => _result.Spell(_model).ShouldEqual("Subject");
    [Fact] void should_keep_the_framework_collision_aliased() => _result.Spell(_framework).ShouldEqual("StageImport0::Subject");
    [Fact] void should_emit_the_used_namespace_alias() => Assert.Equal<string>(["StageImport0 = global::System.Reactive.Subjects"], _result.Aliases);
    [Fact] void should_not_rely_on_the_absent_scaffold_file() => _result.TopLevelUsings.ShouldBeEmpty();
}
