// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { endpoints } from '../../endpoints';
import type { Detail } from '../../Detail';
import type { Summary } from '../../Summary';
import { detail, model, response, serve } from './a_browser';

export const anotherModel: Summary = { ...model, id: 'sm:another', name: 'AnotherSummary' };
export const anotherDetail: Detail = {
    ...detail, id: anotherModel.id, name: anotherModel.name,
    instances: [{ key: 'another-project', values: { name: 'Another project' } }],
};

export function serveTwoModels() {
    const requests = serve([model, anotherModel]);
    requests.mockImplementation(input => {
        if (String(input) === endpoints.readModels) return Promise.resolve(response({ readModels: [model, anotherModel] }));
        return Promise.resolve(response(String(input) === endpoints.readModel(model.id) ? detail : anotherDetail));
    });
    return requests;
}
