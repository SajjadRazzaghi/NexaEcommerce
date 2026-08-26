import { ApiError, type ProblemDetails } from '@/lib/problem';

const BASE = '/api';

type Method = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface RequestOptions {
  signal?: AbortSignal;
  params?: Record<string, unknown>;
}

function buildUrl(path: string, params?: Record<string, unknown>) {
  if (!params || Object.keys(params).length === 0) return `${BASE}${path}`;

  const separator = path.includes('?') ? '&' : '?';
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `${BASE}${path}${separator}${query}` : `${BASE}${path}`;
}

async function request<T>(
  method: Method,
  path: string,
  body?: unknown,
  options?: RequestOptions,
): Promise<T> {
  const response = await fetch(buildUrl(path, options?.params), {
    method,
    credentials: 'include',
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
    signal: options?.signal,
  });

  if (response.status === 204) return undefined as T;

  const isJson = response.headers.get('content-type')?.includes('json') ?? false;
  const data = isJson ? await response.json().catch(() => null) : null;

  if (!response.ok) {
    const problem: ProblemDetails =
      data && typeof data === 'object' && 'status' in data
        ? (data as ProblemDetails)
        : { status: response.status, title: response.statusText };

    throw new ApiError(problem);
  }

  return data as T;
}

export const api = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>('GET', path, undefined, options),

  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('POST', path, body, options),

  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('PUT', path, body, options),

  patch: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>('PATCH', path, body, options),

  del: <T>(path: string, options?: RequestOptions) =>
    request<T>('DELETE', path, undefined, options),

  delete: <T>(path: string, options?: RequestOptions) =>
    request<T>('DELETE', path, undefined, options),
};
