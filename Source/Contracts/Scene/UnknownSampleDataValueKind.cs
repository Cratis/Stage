// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The exception that is thrown when a sample-data field has a value kind that <see cref="SampleDataConverter"/> does not know how to convert.
/// </summary>
/// <param name="kind">The unrecognized sample-data value kind.</param>
public class UnknownSampleDataValueKind(SampleDataValueKind kind) : Exception($"'{kind}' is not a known sample-data value kind");
