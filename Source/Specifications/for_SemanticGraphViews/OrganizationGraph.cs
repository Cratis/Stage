// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.Graphs;

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
namespace Cratis.Stage.Specifications.for_SemanticGraphViews;

/// <summary>
/// A person of the Organization graph the shared query vectors seed.
/// </summary>
/// <param name="PersonId">The key.</param>
/// <param name="Name">The name.</param>
[Graph("Organization")]
[Node]
public record Person([Key] Guid PersonId, string Name);

/// <summary>
/// A group of the Organization graph the shared query vectors seed.
/// </summary>
/// <param name="GroupId">The key.</param>
/// <param name="Name">The name.</param>
[Graph("Organization")]
[Node]
public record Group([Key] Guid GroupId, string Name);

/// <summary>
/// A person's membership of a group.
/// </summary>
/// <param name="PersonId">The member.</param>
/// <param name="GroupId">The group.</param>
/// <param name="Since">When the membership began.</param>
[Graph("Organization")]
[Edge("MEMBER_OF")]
public record MemberOf([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, DateTimeOffset Since);

/// <summary>
/// A group nested in another.
/// </summary>
/// <param name="ChildId">The nested group.</param>
/// <param name="ParentId">The group it is part of.</param>
[Graph("Organization")]
[Edge("PART_OF")]
public record PartOf([Source<Group>] Guid ChildId, [Target<Group>] Guid ParentId);

/// <summary>
/// A role a person holds in a group, identified by its assignment.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="GroupId">The group.</param>
/// <param name="AssignmentId">The identity of the assignment.</param>
/// <param name="Role">The role.</param>
/// <param name="Since">When the role was assigned.</param>
[Graph("Organization")]
[Edge("HAS_ROLE")]
public record HasRole([Source<Person>] Guid PersonId, [Target<Group>] Guid GroupId, [Key] Guid AssignmentId, string Role, DateTimeOffset Since);
