// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { ProjectResponse } from './ProjectResponse';

/** The same native proxy contract a generated app registers through bindings.ts. */
export class RegisterProject extends Command {
    readonly route = 'api/projects/register';
    readonly propertyDescriptors = [new PropertyDescriptor('name', String)];
    get requestParameters(): string[] { return []; }
    private _name?: string;
    get name() { return this._name; }
    set name(value: string | undefined) { this._name = value; this.propertyChanged('name'); }
    constructor() { super(ProjectResponse, false); }
}
