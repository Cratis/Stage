// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedOptions;

public class when_the_storage_type_is_not_supported : a_published_configuration
{
    IReadOnlyList<string> _problems = null!;

    void Establish() => _values["Stage:Published:Storage:Type"] = "Oracle";

    void Because() => _problems = Options().Problems();

    [Fact] void should_refuse_the_type() => _problems.Any(problem => problem.Contains("Storage type 'Oracle' is not supported")).ShouldBeTrue();
    [Fact] void should_name_the_accepted_types() => _problems.Any(problem => problem.Contains("MongoDB, PostgreSql, MsSql")).ShouldBeTrue();
}
