// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// The exception that is thrown when the engine is started without a Screenplay <c>.play</c> file or folder path.
/// </summary>
public class MissingModelArgument : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingModelArgument"/> class.
    /// </summary>
    public MissingModelArgument()
        : base("No event model was provided. Pass the path to a Screenplay .play file or a directory containing .play files as the first argument.")
    {
    }
}
