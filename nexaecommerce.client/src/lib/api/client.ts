import { ApiError, type ProblemDetails } from '@/lib/problem';

const BASE = '/api';

type Method = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface RequestOptions {
  signal?: AbortSignal;
  params?: Record<string, unknown>;
}

function buildUrl(
    path: string,
    params?: Record<string, unknown>,
) {
    if (
        !params ||
        Object.keys(params).length === 0
    ) {
        return `${BASE}${path}`;
    }

    const separator = path.includes('?')
        ? '&'
        : '?';

    const search = new URLSearchParams();

    for (const [
        key,
        value,
    ] of Object.entries(params)) {
        if (
            value === undefined ||
            value === null ||
            value === ''
        ) {
            continue;
        }

        search.set(
            key,
            String(value),
        );
    }

    const query =
        search.toString();

    return query
        ? `${BASE}${path}${separator}${query}`
        : `${BASE}${path}`;
}
function isRecord(
  value: unknown,
): value is Record<string, unknown> {
  return (
    typeof value === 'object' &&
    value !== null
  );
}

function toProblemDetails(
  data: unknown,
  response: Response,
): ProblemDetails {
  if (!isRecord(data)) {
    return {
      status: response.status,
      title:
        response.statusText ||
        'Request failed.',
    };
  }

  const status =
    typeof data.status === 'number'
      ? data.status
      : response.status;

  const title =
    typeof data.title === 'string'
      ? data.title
      : undefined;

  const detail =
    typeof data.detail === 'string'
      ? data.detail
      : undefined;

  const error =
    typeof data.error === 'string'
      ? data.error
      : undefined;

  const message =
    typeof data.message === 'string'
      ? data.message
      : undefined;

  const code =
    typeof data.code === 'string'
      ? data.code
      : undefined;

  const traceId =
    typeof data.traceId === 'string'
      ? data.traceId
      : undefined;

  const instance =
    typeof data.instance === 'string'
      ? data.instance
      : undefined;

  const type =
    typeof data.type === 'string'
      ? data.type
      : undefined;

  let errors:
    | Record<string, string[]>
    | undefined;

  if (
    isRecord(data.errors)
  ) {
    const parsedErrors: Record<
      string,
      string[]
    > = {};

    for (const [
      field,
      messages,
    ] of Object.entries(
      data.errors,
    )) {
      if (
        Array.isArray(messages)
      ) {
        parsedErrors[field] =
          messages.filter(
            (
              item,
            ): item is string =>
              typeof item ===
              'string',
          );
      } else if (
        typeof messages ===
        'string'
      ) {
        parsedErrors[field] =
          [messages];
      }
    }

    if (
      Object.keys(
        parsedErrors,
      ).length > 0
    ) {
      errors =
        parsedErrors;
    }
  }

  return {
    status,
    title,
    detail,
    error,
    message,
    code,
    traceId,
    instance,
    type,
    errors,
  };
}

async function request<T>(
  method: Method,
  path: string,
  body?: unknown,
  options?: RequestOptions,
): Promise<T> {
  const response =
    await fetch(
      buildUrl(
        path,
        options?.params,
      ),
      {
        method,
        credentials: 'include',
        headers:
          body !== undefined
            ? {
                'Content-Type':
                  'application/json',
                Accept:
                  'application/json',
              }
            : {
                Accept:
                  'application/json',
              },
        body:
          body !== undefined
            ? JSON.stringify(body)
            : undefined,
        signal:
          options?.signal,
      },
    );

  if (
    response.status === 204
  ) {
    return undefined as T;
  }

  const contentType =
    response.headers.get(
      'content-type',
    ) ?? '';

  const isJson =
    contentType
      .toLowerCase()
      .includes('json');

  const data = isJson
    ? await response
        .json()
        .catch(
          () => null,
        )
    : null;

  if (!response.ok) {
    const problem =
      toProblemDetails(
        data,
        response,
      );

    throw new ApiError(
      problem,
    );
  }

  return data as T;
}

export const api = {
  get: <T>(
    path: string,
    options?: RequestOptions,
  ) =>
    request<T>(
      'GET',
      path,
      undefined,
      options,
    ),

  post: <T>(
    path: string,
    body?: unknown,
    options?: RequestOptions,
  ) =>
    request<T>(
      'POST',
      path,
      body,
      options,
    ),

  put: <T>(
    path: string,
    body?: unknown,
    options?: RequestOptions,
  ) =>
    request<T>(
      'PUT',
      path,
      body,
      options,
    ),

  patch: <T>(
    path: string,
    body?: unknown,
    options?: RequestOptions,
  ) =>
    request<T>(
      'PATCH',
      path,
      body,
      options,
    ),

  del: <T>(
    path: string,
    options?: RequestOptions,
  ) =>
    request<T>(
      'DELETE',
      path,
      undefined,
      options,
    ),

  delete: <T>(
    path: string,
    options?: RequestOptions,
  ) =>
    request<T>(
      'DELETE',
      path,
      undefined,
      options,
    ),
};

