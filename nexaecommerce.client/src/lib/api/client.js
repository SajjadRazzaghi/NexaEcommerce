import { ApiError } from '@/lib/problem';
const BASE = '/api';
async function request(method, path, body, options) {
    const response = await fetch(`${BASE}${path}`, {
        method,
        // Cookie auth: always send credentials so the auth cookie rides along.
        credentials: 'include',
        headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
        body: body !== undefined ? JSON.stringify(body) : undefined,
        signal: options?.signal,
    });
    if (response.status === 204)
        return undefined;
    const isJson = response.headers.get('content-type')?.includes('json') ?? false;
    const data = isJson ? await response.json().catch(() => null) : null;
    if (!response.ok) {
        const problem = data && typeof data === 'object' && 'status' in data
            ? data
            : { status: response.status, title: response.statusText };
        throw new ApiError(problem);
    }
    return data;
}
export const api = {
    get: (path, options) => request('GET', path, undefined, options),
    post: (path, body) => request('POST', path, body),
    put: (path, body) => request('PUT', path, body),
    patch: (path, body) => request('PATCH', path, body),
    del: (path) => request('DELETE', path),
};
