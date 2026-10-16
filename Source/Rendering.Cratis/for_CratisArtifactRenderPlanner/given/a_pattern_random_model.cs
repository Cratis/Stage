// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class a_pattern_random_model
{
    public const string Source = """
        concept Reference : String
          generate pattern "REF-?{16}"
        concept DatedReference : String
          generate pattern "{yyyy}-{MM}-{dd}-?{16}"
        concept CheckedReference : String
          generate pattern "#{16}" checksum luhn
          validate checksum luhn
        concept Ticket : Int
          generate random -9223372036854775808..9223372036854775807
        module Work
          feature Issue
            slice StateChange Start
              command Create
                reference Reference generated
                datedReference DatedReference generated
                checkedReference CheckedReference generated
                ticket Ticket generated
                returns reference
              specification PinnedStrategies
                given clock "2028-02-29T23:59:59Z"
                when Create
                  generated reference = "REF-ABCDEFGHIJKLMNOP"
                  generated datedReference = "2028-02-29-ABCDEFGHIJKLMNOP"
                  generated checkedReference = "00000000000000000"
                  generated ticket = 7
                then returns "REF-ABCDEFGHIJKLMNOP"
        """;

    public static ExecutableSemanticModel Model() => invoice_model.Compile(Source);
}
