// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_starting_the_eventmodel_host_without_testing_dependencies : when_starting_with_the_shared_executor
{
    protected override bool Semantic => false;
}
