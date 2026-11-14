// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;

/// <summary>
/// Serializes every user of the process-cached offline authority under its sole lifetime owner.
/// </summary>
[CollectionDefinition(nameof(offline_authentication_collection), DisableParallelization = true)]
public class offline_authentication_collection : ICollectionFixture<OfflineAuthenticationFixture>;
