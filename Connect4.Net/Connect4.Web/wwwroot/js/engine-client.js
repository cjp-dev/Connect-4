// The page's side of the engine worker: starts and terminates it and matches responses to requests.
const workerUrl = new URL('./engine-worker.js', import.meta.url);

// Resolved through the page's import map, so the worker gets the fingerprinted file.
const dotnetUrl = import.meta.resolve('../_framework/dotnet.js');

// Lets the page stop a running search without terminating the worker (needs cross-origin isolation).
const canShareMemory = typeof SharedArrayBuffer !== 'undefined' && self.crossOriginIsolated;
const signalBuffer = canShareMemory ? new SharedArrayBuffer(4) : null;
const signalView = signalBuffer ? new Int32Array(signalBuffer) : null;

let worker = null;
let nextId = 1;
const pending = new Map();

function rejectPending(message) {
    for (const { reject } of pending.values()) {
        reject(new Error(message));
    }
    pending.clear();
}

function post(command, args, includeBuffer = false) {
    const id = nextId++;
    const response = new Promise((resolve, reject) => pending.set(id, { resolve, reject }));
    const message = { id, command, args };
    if (includeBuffer && signalBuffer) {
        message.signalBuffer = signalBuffer;
    }
    worker.postMessage(message);
    return response;
}

// Starts a new worker with its hash tables; resolves when it is ready.
export function start(progressTarget, hashLogSize, endgameLogSize) {
    terminate();
    const url = new URL(workerUrl);
    url.searchParams.set('dotnet', dotnetUrl);
    const current = new Worker(url, { type: 'module' });

    current.addEventListener('message', e => {
        if (current !== worker) {
            return;
        }
        const message = e.data;
        if (message.type === 'progress') {
            progressTarget.invokeMethodAsync('OnProgress', message.json);
            return;
        }
        const request = pending.get(message.id);
        if (request) {
            pending.delete(message.id);
            if (message.error !== undefined) {
                request.reject(new Error(message.error));
            } else {
                request.resolve(message.result);
            }
        }
    });
    current.addEventListener('error', e => {
        if (current === worker) {
            rejectPending(`The engine could not run: ${e.message ?? 'unknown error'}`);
        }
    });

    worker = current;
    return post('init', [hashLogSize, endgameLogSize], true);
}

export function call(command, ...args) {
    if (!worker) {
        return Promise.reject(new Error('The engine is not running.'));
    }
    if (command === 'chooseMove' && signalView) {
        Atomics.store(signalView, 0, 0);
    }
    return post(command, args);
}

// Asks a running search to stop (1 = Move Now, 2 = cancel); false if the worker can only be terminated.
export function signal(code) {
    if (!signalView) {
        return false;
    }
    Atomics.store(signalView, 0, code);
    return true;
}

// Force-stops the worker when the signal is not available or not answered in time.
export function terminate() {
    worker?.terminate();
    worker = null;
    rejectPending('The engine was stopped.');
}
