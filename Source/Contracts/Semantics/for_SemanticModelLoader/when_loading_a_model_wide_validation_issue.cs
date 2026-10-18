// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_a_model_wide_validation_issue : given.a_source_tree
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "nested", "source.play"), """
        concept Code : String
          validate
            matches "(?=x)"
        module Codes
          feature Registration
            slice StateChange Register
              command Register
                code Code identifier
                produces Registered
                  for code
              event Registered
        """);

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Codes");

    [Fact] void should_refuse_the_model() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_model_wide_compiler_issue_blocking() => Assert.Contains(_result.Errors, error => error.Code == "PLAY0699");
}
#endif
