// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// A minimal extension backend: one safe ability that greets, with a public setting for the greeting.
// Stage connects to it at the address its configuration points to (Stage:Extensions:Backends:Hello:Endpoint).
package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net"

	"github.com/Cratis/Stage/Source/Extensions/Go/extension"
)

func main() {
	hello := extension.New("hello", "1.0.0").
		Setting("greeting", false).
		Ability("Greet", extension.Safe, func(ctx context.Context, call *extension.Call) (extension.Outcome, error) {
			greeting, ok, err := call.Host.Setting(ctx, "greeting")
			if err != nil {
				return extension.Outcome{}, err
			}
			if !ok {
				greeting = "Hello"
			}
			var input struct {
				Name string `json:"name"`
			}
			if err := json.Unmarshal([]byte(call.Input), &input); err != nil {
				return extension.NotSent("the input is not JSON"), nil
			}
			output, _ := json.Marshal(map[string]string{"message": fmt.Sprintf("%s, %s!", greeting, input.Name)})
			return extension.Returned(string(output), ""), nil
		})

	listener, err := net.Listen("tcp", ":50051")
	if err != nil {
		log.Fatal(err)
	}
	log.Fatal(hello.Serve(listener))
}
