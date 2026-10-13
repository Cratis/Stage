#!/usr/bin/env python3
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
"""Probe a fresh generated screens application; the caller owns its host and kernel.

Usage: verify-observable-query.py BASE_URL EVIDENCE_DIRECTORY
The generated-react browser harness runs this before its existing Playwright scenarios.
"""
import json
from pathlib import Path
import sys
import threading
import time
from urllib.request import Request, urlopen
import uuid


class Stream:
    def __init__(self, url, path):
        self.frames = []
        self.error = None
        self.condition = threading.Condition()
        self.path = path
        self.response = None
        self.thread = threading.Thread(target=self.read, args=(url,), daemon=True)
        self.thread.start()

    def read(self, url):
        try:
            self.response = urlopen(Request(url, headers={'Accept': 'text/event-stream'}), timeout=10)
            with self.path.open('w') as evidence:
                for raw in self.response:
                    line = raw.decode('utf-8')
                    evidence.write(line)
                    evidence.flush()
                    if line.startswith('data:'):
                        with self.condition:
                            self.frames.append(json.loads(line[5:]))
                            self.condition.notify_all()
        except Exception as error:
            with self.condition:
                self.error = error
                self.condition.notify_all()

    def wait(self, predicate):
        deadline = time.monotonic() + 10
        with self.condition:
            while not predicate(self.frames):
                if self.error is not None:
                    raise self.error
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    raise TimeoutError(f'SSE predicate did not hold; see {self.path}')
                self.condition.wait(remaining)

    def close(self):
        if self.response is not None:
            self.response.close()
        self.thread.join(timeout=11)


def post(base, route, body):
    request = Request(base + route, data=json.dumps(body).encode(), headers={'Content-Type': 'application/json'}, method='POST')
    with urlopen(request, timeout=10) as response:
        result = json.load(response)
        assert result.get('isSuccess') is not False, result


def contains(frames, text):
    return any(text in json.dumps(frame.get('data')) for frame in frames)


def main(base, evidence):
    evidence.mkdir(parents=True, exist_ok=True)
    lists = base + '/api/workspaces/tracking/work-item-list/all-work-items'
    with urlopen(lists, timeout=10) as response:
        assert response.status == 202, response.status
        (evidence / 'plain-get.json').write_bytes(response.read())
    with urlopen(lists + '?waitForFirstResult=true', timeout=35) as response:
        assert response.status == 200, response.status
        result = json.load(response)
        assert result['data'] == [], result
        (evidence / 'first-result.json').write_text(json.dumps(result))
    a, b = str(uuid.uuid4()), str(uuid.uuid4())
    title = 'Observable push ' + a
    comments = None
    stream = Stream(lists, evidence / 'all-work-items.sse')
    try:
        stream.wait(lambda frames: len(frames) >= 1)
        post(base, '/api/workspaces/tracking/create-work-item', {'workItemId': a, 'title': title})
        stream.wait(lambda frames: len(frames) >= 2 and contains(frames[1:], title))
        post(base, '/api/workspaces/tracking/create-work-item', {'workItemId': b, 'title': 'Other stream'})
        comments = Stream(base + '/api/workspaces/tracking/work-item-comments/comments-for-work-item?workItemId=' + a, evidence / 'comments-for-a.sse')
        comments.wait(lambda frames: len(frames) >= 1)
        excluded = 'Excluded comment ' + b
        included = 'Included comment ' + a
        post(base, '/api/workspaces/tracking/add-comment', {'commentId': str(uuid.uuid4()), 'workItemId': b, 'text': excluded})
        post(base, '/api/workspaces/tracking/add-comment', {'commentId': str(uuid.uuid4()), 'workItemId': a, 'text': included})
        comments.wait(lambda frames: contains(frames, included))
        assert not contains(comments.frames, excluded), comments.frames
        print('Observable queries: 202, waited empty snapshot, list push and keyed SSE isolation passed')
    finally:
        if comments is not None:
            comments.close()
        stream.close()


if __name__ == '__main__':
    main(sys.argv[1].rstrip('/'), Path(sys.argv[2]))
