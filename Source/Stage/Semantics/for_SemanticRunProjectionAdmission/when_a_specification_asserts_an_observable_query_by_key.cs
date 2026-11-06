// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunProjectionAdmission;

public class when_a_specification_asserts_an_observable_query_by_key : Specification
{
    const string Source = """
        concept PersonId : Uuid
        concept PersonName : String

        module People
          feature Directory
            slice StateChange RegisterPerson
              command RegisterPerson
                personId PersonId identifier
                name     PersonName
                produces PersonRegistered
                  name = name

              event PersonRegistered
                name PersonName

            slice StateView Directory

              readmodel Person
                personId PersonId key
                name     PersonName

              projection People => Person
                from PersonRegistered

              query GetPerson => observable Person optional
                by personId PersonId

              query ListPeople => observable Person[]

              specification SeeingAPerson
                given PersonRegistered
                  for "11111111-1111-4111-8111-111111111111"
                  name = "Ada"
                then query GetPerson
                  arguments
                    personId = "11111111-1111-4111-8111-111111111111"
                  result
                    name = "Ada"

              specification SeeingEveryone
                given PersonRegistered
                  for "11111111-1111-4111-8111-111111111111"
                  name = "Ada"
                then query ListPeople
                  result
                    personId = "11111111-1111-4111-8111-111111111111"
                    name = "Ada"
        """;

    Dictionary<string, string> _statuses = [];

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("People"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("people"), "people", "People.play", Source);
        var compilation = new SemanticModelCompiler().Compile("People", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = ExecutableSemanticModel.Create(compilation.Value.Model.LanguageVersion, compilation.Value.Model.SemanticVersion, compilation.Value.Model.Application);
        var names = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Specifications).ToDictionary(specification => specification.Id.ToString(), specification => specification.Name);
        _statuses = new SemanticRuntimeAdmission(SemanticExecutionPlan.Compile(model).Plan, _ => null, null).Entries
            .Where(entry => entry.Kind == "specification")
            .ToDictionary(entry => names[entry.Artifact], entry => entry.Status);
    }

    [Fact] void should_admit_the_specification_that_asserts_the_observable_query_by_key() => _statuses["SeeingAPerson"].ShouldEqual("supported");
    [Fact] void should_still_refuse_the_unfiltered_observable_collection() => _statuses["SeeingEveryone"].ShouldEqual("unsupported");
}
