// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies.given;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticImplementationBodies;

public class when_resolving_the_same_source_as_the_loader : a_compiled_document_set
{
    const string Content = "return context.Value > 0;";

    string _folder = null!;
    LoadedSemanticModel _loaded = null!;
    ImmutableDictionary<string, string> _bodies = null!;

    async Task Establish()
    {
        _folder = Directory.CreateTempSubdirectory("stage-implementation-bodies").FullName;
        Directory.CreateDirectory(Path.Combine(_folder, "Rules"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "Orders.play"), FileSource);
        await File.WriteAllTextAsync(Path.Combine(_folder, "Rules", "Positive.cs"), Content);
        Compile(FileSource, ImmutableDictionary<string, string>.Empty.Add("Rules/Positive.cs", Content));
    }

    async Task Because()
    {
        _loaded = await SemanticModelLoader.LoadFromPathAsync(_folder, null, "Orders");
        _bodies = SemanticImplementationBodies.Resolve(_documents, _requirements);
    }

    [Fact] void should_resolve_the_same_requirements_as_the_loader() => _bodies.Keys.Order(StringComparer.Ordinal).SequenceEqual(_loaded.ImplementationContents.Keys.Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_resolve_the_same_contents_as_the_loader() => _loaded.ImplementationContents.All(pair => _bodies.TryGetValue(pair.Key, out var body) && string.Equals(body, pair.Value, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_resolve_something_to_compare() => _loaded.ImplementationContents.Count.ShouldEqual(1);

    void Destroy() => Directory.Delete(_folder, recursive: true);
}
