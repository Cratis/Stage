// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_a_reachable_translate_reaction : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        const string source = """
            module Work
              feature Work
                slice StateChange Work
                  event Saved
                  event Followed
                  command Save
                    id String identifier
                    produces Saved
                      for id
                  specification Saving
                    when Save
                      id = "one"
                    then Saved
                      for "one"
                slice Translate LegacySync
                  reaction Settler
                    when Saved
                      produces Followed
                        for "one"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("work"), "work", "Work.play", source);
        var compilation = new SemanticModelCompiler().Compile("Work", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_specification() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("unsupported");
    [Fact] void should_preserve_the_translate_diagnostic() => _admission.Entries.Single(entry => entry.Kind == "specification").Details!.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
}
