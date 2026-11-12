// The notice board component of the Notifier extension. A screen places it with `component Notifier.NoticeBoard`.
// `react`, `react-dom` and `@cratis/scene.extensions` come from the Stage page's import map, so this module bundles none of them.
import { createElement, useState } from 'react';
import { reactComponent, useInvoke, useRaise, useSettings } from '@cratis/scene.extensions';

function NoticeBoardView({ title }) {
    const invoke = useInvoke();
    const raise = useRaise();
    const settings = useSettings();
    const [status, setStatus] = useState('');

    const post = async () => {
        const answer = await invoke('Notify', { message: `A notice for ${settings.channel}` });
        setStatus(answer.outcome);
    };

    return createElement('section', { 'aria-label': title },
        createElement('h2', null, title),
        createElement('button', { type: 'button', onClick: post }, `Post to ${settings.channel}`),
        createElement('button', { type: 'button', onClick: () => raise('dismissed', { noticeId: status }) }, 'Dismiss'),
        createElement('p', { role: 'status' }, status));
}

export const NoticeBoard = reactComponent(NoticeBoardView);
