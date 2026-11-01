// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_ImportResolver;

public class when_resolving_for_a_scope_without_scaffold_global_usings : Specification
{
    readonly TypeReference _builder = new("Cratis.Chronicle.Keys", "KeyBuilder", 1, ReferenceContext.Type);
    readonly TypeReference _subject = new("System.Reactive.Subjects", "Subject", 1, ReferenceContext.Type);
    ImportResolution _application = null!;
    ImportResolution _scoped = null!;

    void Because()
    {
        var request = new ImportRequest("Shop.Orders.Placing", GeneratedProjects.Application, [], [_builder, _subject]) { RequiredImports = ["System.Reactive.Subjects"] };
        _application = ImportResolver.Resolve(request, FrameworkNameCatalog.Current);
        _scoped = ImportResolver.Resolve(request with { HasScaffoldGlobalUsings = false }, FrameworkNameCatalog.Current);
    }

    [Fact] void should_import_nothing_with_the_scaffold_file() => _application.TopLevelUsings.ShouldBeEmpty();
    [Fact] void should_import_the_scaffold_namespaces_without_it() => string.Join(',', _scoped.TopLevelUsings).ShouldEqual("Cratis.Chronicle.Keys,System.Reactive.Subjects");
    [Fact] void should_spell_the_builder_the_same() => _scoped.Spell(_builder).ShouldEqual(_application.Spell(_builder));
    [Fact] void should_spell_the_subject_the_same() => _scoped.Spell(_subject).ShouldEqual(_application.Spell(_subject));
    [Fact] void should_spell_both_short() => (_scoped.Spell(_builder), _scoped.Spell(_subject)).ShouldEqual(("KeyBuilder", "Subject"));
}
