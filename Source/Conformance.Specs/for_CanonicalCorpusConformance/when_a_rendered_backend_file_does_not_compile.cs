// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_CanonicalCorpusConformance;

public class when_a_rendered_backend_file_does_not_compile : a_rendered_canonical_vector
{
    IReadOnlyList<compile_error> _errors = null!;
    string _path = null!;

    void Because()
    {
        _path = _plan.Artifacts.First(file => file.RelativePath.EndsWith("/PutInBasket.cs", StringComparison.Ordinal)).RelativePath;
        _errors = rendered_compilation.Errors(Corrupt(_path));
    }

    [Fact] void should_report_the_first_error_first() => _errors[0].Code.ShouldEqual("CS0246");
    [Fact] void should_report_the_backend_path() => _errors[0].Path.ShouldEqual(_path);
}
#endif
