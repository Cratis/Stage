// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Button } from 'primereact/button';
import { useBrowser } from './useBrowser';
import './browser.css';
import '../app.css';

function json(value: unknown): string {
    if (value === undefined) return '';
    return value !== null && typeof value === 'object' ? JSON.stringify(value) : String(value);
}

/** Browses every read model in a semantic session without using Chronicle projection mirrors. */
export function Browser() {
    const browser = useBrowser();

    return (
        <main className='read-model-browser'>
            <header className='read-model-browser__header'>
                <h1>Read models</h1>
                <Button type='button' onClick={browser.refresh}>Refresh</Button>
            </header>
            {browser.loading && <p role='status'>Loading read models…</p>}
            {browser.message && <p className='stage-note stage-note--error' role='alert'>{browser.message}</p>}
            {browser.readModels && (
                <section className='stage-table'>
                    <table aria-label='Read models'>
                        <thead><tr><th scope='col'>Name</th><th scope='col'>Location</th><th scope='col'>Instances</th></tr></thead>
                        <tbody>
                            {browser.readModels.map(model => (
                                <tr key={model.id} aria-selected={browser.selectedId === model.id}>
                                    <td><Button type='button' onClick={() => browser.select(model.id)}>{model.name}</Button></td>
                                    <td>{[model.module, model.feature, model.slice].join(' / ')}</td>
                                    <td>{model.instances}</td>
                                </tr>
                            ))}
                            {browser.readModels.length === 0 && <tr><td colSpan={3}>No read models in this session.</td></tr>}
                        </tbody>
                    </table>
                </section>
            )}
            {browser.detail && (
                <section className='stage-table'>
                    <h2>{browser.detail.name}</h2>
                    <table aria-label={`${browser.detail.name} instances`}>
                        <thead><tr>
                            <th scope='col'>Key</th>
                            {browser.detail.properties.map(property => <th key={property.name} scope='col' title={property.type}>{property.name}</th>)}
                        </tr></thead>
                        <tbody>
                            {browser.detail.instances.map((instance, index) => (
                                <tr key={index}>
                                    <td>{json(instance.key)}</td>
                                    {browser.detail!.properties.map(property => <td key={property.name}>{json(instance.values[property.name])}</td>)}
                                </tr>
                            ))}
                            {browser.detail.instances.length === 0 && <tr><td colSpan={browser.detail.properties.length + 1}>No instances yet.</td></tr>}
                        </tbody>
                    </table>
                </section>
            )}
        </main>
    );
}
