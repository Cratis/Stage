// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Stage.Runtime.for_RootStringKeyContract;

[ReadModel]
[FromEvent<RootStringKeyContractUpdated>(ConstantKey = " /tenant:global_*+.- ")]
public record RootStringKeyContractPunctuation([SetFrom<RootStringKeyContractUpdated>(nameof(RootStringKeyContractUpdated.Status))] string Status);
#endif
