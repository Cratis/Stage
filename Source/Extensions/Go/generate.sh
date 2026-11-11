#!/usr/bin/env bash
# Regenerates the Go bindings of the extension protocol into protocol/v1.
# Needs protoc, protoc-gen-go and protoc-gen-go-grpc on the PATH:
#   go install google.golang.org/protobuf/cmd/protoc-gen-go@v1.36.11
#   go install google.golang.org/grpc/cmd/protoc-gen-go-grpc@latest
set -euo pipefail
cd "$(dirname "$0")"
rm -rf protocol
mkdir -p protocol
protoc --proto_path=../Protocol \
  --go_out=protocol --go_opt=module=github.com/Cratis/Stage/Source/Extensions/Go/protocol \
  --go-grpc_out=protocol --go-grpc_opt=module=github.com/Cratis/Stage/Source/Extensions/Go/protocol \
  ../Protocol/cratis/stage/extensions/v1/*.proto
