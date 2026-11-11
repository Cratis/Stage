// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

package extension

import (
	"context"
	"net"
	"time"

	protocol "github.com/Cratis/Stage/Source/Extensions/Go/protocol/v1"
	"google.golang.org/grpc"
)

// Call is one attempt at an ability, as the handler receives it.
type Call struct {
	// Tenant is the tenant the work runs for.
	Tenant string
	// System is the system whose ability is performed.
	System string
	// InvocationID is the invocation identity.
	InvocationID string
	// IdempotencyKey is the key the effect is deduplicated on. It is the same for every attempt of the invocation.
	IdempotencyKey string
	// Attempt is the attempt number, starting at 1.
	Attempt int
	// Deadline is when the attempt runs out of time; the zero time means none was imposed.
	Deadline time.Time
	// Causation is what caused the invocation.
	Causation string
	// Traceparent is the W3C trace context, so the extension's own telemetry joins the invocation's trace.
	Traceparent string
	// Input is the ability's input, as JSON.
	Input string
	// Host reaches Stage's host services, tied to this invocation.
	Host *Host
}

// AbilityFunc performs one attempt at an ability. An error is a failure that does not rule the effect out.
type AbilityFunc func(ctx context.Context, call *Call) (Outcome, error)

// HTTPCall is an inbound HTTP request Stage routed to the extension. Cookies and authorization are never forwarded.
type HTTPCall struct {
	Method  string
	Path    string
	Query   string
	Headers map[string][]string
	Body    []byte
}

// HTTPReply is the answer to an HTTPCall.
type HTTPReply struct {
	Status  int
	Body    []byte
	Headers map[string][]string
}

// HTTPFunc handles an inbound HTTP request.
type HTTPFunc func(ctx context.Context, call *HTTPCall) (HTTPReply, error)

type ability struct {
	name     string
	effect   EffectClass
	outcomes []string
	handler  AbilityFunc
}

type route struct {
	prefix  string
	handler HTTPFunc
}

type setting struct {
	name     string
	secret   bool
	required bool
}

// Extension describes an extension backend: what it realizes and how.
type Extension struct {
	name      string
	version   string
	abilities []ability
	triggers  []string
	settings  []setting
	routes    []route
}

// New starts describing an extension.
func New(name string, version string) *Extension {
	return &Extension{name: name, version: version}
}

// Ability declares an ability the extension realizes. Stage checks it against the contract of the system at the
// handshake, so effect and outcomes must be what the contract declares.
func (e *Extension) Ability(name string, effect EffectClass, handler AbilityFunc, outcomes ...string) *Extension {
	kept := e.abilities[:0]
	for _, existing := range e.abilities {
		if existing.name != name {
			kept = append(kept, existing)
		}
	}
	e.abilities = append(kept, ability{name: name, effect: effect, outcomes: outcomes, handler: handler})
	return e
}

// Trigger declares a trigger the extension raises.
func (e *Extension) Trigger(name string) *Extension {
	e.triggers = append(e.triggers, name)
	return e
}

// Setting declares a public setting the extension reads.
func (e *Extension) Setting(name string, required bool) *Extension {
	e.settings = append(e.settings, setting{name: name, required: required})
	return e
}

// Secret declares a secret the extension reads. It is delivered by the host services and nowhere else.
func (e *Extension) Secret(name string, required bool) *Extension {
	e.settings = append(e.settings, setting{name: name, secret: true, required: required})
	return e
}

// HTTP handles inbound HTTP requests below a path prefix, such as a webhook.
func (e *Extension) HTTP(prefix string, handler HTTPFunc) *Extension {
	e.routes = append(e.routes, route{prefix: prefix, handler: handler})
	return e
}

// NewServer creates the gRPC server that speaks the extension protocol for the extension.
func (e *Extension) NewServer(options ...grpc.ServerOption) *grpc.Server {
	server := grpc.NewServer(options...)
	protocol.RegisterExtensionBackendServer(server, newBackend(e))
	return server
}

// Serve serves the extension on the listener until the server stops.
func (e *Extension) Serve(listener net.Listener) error {
	return e.NewServer().Serve(listener)
}
