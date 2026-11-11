// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Package extension is the Go SDK for Cratis Stage extension backends.
//
// Declare what the extension realizes with New, then serve it with Extension.Serve. Stage connects, checks the
// running backend against the contract of its system, and calls it; the extension reaches Stage's host services
// (settings, secrets, triggers, answers, logs and metrics) through the Host of each Call.
package extension
