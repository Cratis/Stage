// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A minimal extension backend: one safe ability that greets, with a public setting for the greeting.
// Stage connects to it at the address its configuration points to (Stage:Extensions:Backends:Hello:Endpoint).
package io.cratis.stage.extensions.examples

import com.google.gson.JsonObject
import com.google.gson.JsonParser
import io.cratis.stage.extensions.AbilityOutcome
import io.cratis.stage.extensions.EffectClass
import io.cratis.stage.extensions.Extension

fun main() {
    val hello = Extension.create("hello", "1.0.0")
        .setting("greeting")
        .ability("Greet", EffectClass.Safe) { call ->
            val greeting = call.host.setting("greeting") ?: "Hello"
            val name = JsonParser.parseString(call.input).asJsonObject["name"].asString
            val output = JsonObject()
            output.addProperty("message", "$greeting, $name!")
            AbilityOutcome.Returned(output.toString())
        }

    val server = hello.listen(50051).start()
    println("hello extension listening on ${server.port}")
    server.awaitTermination()
}
