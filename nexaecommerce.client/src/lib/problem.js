/** Thrown by the API client for any non-2xx response, carrying the parsed ProblemDetails. */
export class ApiError extends Error {
    constructor(problem) {
        super(problem.detail || problem.title || 'Something went wrong.');
        Object.defineProperty(this, "problem", {
            enumerable: true,
            configurable: true,
            writable: true,
            value: void 0
        });
        this.name = 'ApiError';
        this.problem = problem;
    }
    get status() {
        return this.problem.status;
    }
    /** Stable machine code (e.g. INVALID_CREDENTIALS) for branching without parsing messages. */
    get code() {
        return this.problem.code;
    }
    get traceId() {
        return this.problem.traceId;
    }
    get fieldErrors() {
        return this.problem.errors;
    }
}
export function isApiError(error) {
    return error instanceof ApiError;
}
