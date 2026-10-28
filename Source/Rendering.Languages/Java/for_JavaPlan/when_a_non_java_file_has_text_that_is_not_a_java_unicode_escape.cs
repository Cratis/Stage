// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_non_java_file_has_text_that_is_not_a_java_unicode_escape : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([File("README.md", new DeclarativeTextSegment(@"\usepackage{example}"), Reference("Outer.Inner", "other"))]);

    [Fact] void should_admit_the_file() => _plan.Files.Length.ShouldEqual(1);
    [Fact] void should_keep_the_reference_canonical() => _plan.Files[0].Segments[1].ShouldEqual(Reference("Outer.Inner", "other"));
}
