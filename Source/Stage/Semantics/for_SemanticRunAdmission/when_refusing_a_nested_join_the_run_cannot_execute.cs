// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Semantics.for_SemanticRunAdmission;

public class when_refusing_a_nested_join_the_run_cannot_execute : Specification
{
    string? _refusal;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NestedJoinAdmission"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("NestedJoinAdmission", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        _refusal = SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())?.Details;
    }

    [Fact] void should_report_the_run_own_refusal() => _refusal.ShouldContain("The spec runner cannot execute this projection shape");
    [Fact] void should_name_the_unsupported_shape() => _refusal.ShouldContain("directly at the read-model root");

    const string Source = """
        type Deep
          reference String
          value String?
        type Link
          reference String
          inner Deep?
        module Billing
          feature Payments
            slice StateView Status
              event Registered
                id String
                name String
              event Detailed
                id String
              event Deeper
                id String
                name String
              event Named
                id String
                name String
              readmodel Summary
                id String key
                details Link?
              projection Summaries => Summary
                from Registered key id
                from Detailed key id
                from Deeper key id
                nested details
                  from Detailed key id
                    reference = id
                  nested inner
                    from Deeper key id
                      reference = id
                      value = name
                    join deepLinked on reference
                      with Named
                        value = name
              specification Existing
                given Registered
                  id = "a"
                  name = "n"
                then readmodel Summary
                  id = "a"
        """;
}
