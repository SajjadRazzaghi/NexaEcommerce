// RFC 7807 ProblemDetails plus the lightweight { error: "..." }
// response shape currently returned by several backend endpoints.

export interface ProblemDetails {
  type?: string;
  title?: string;
  status: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;

  /** Per-field validation messages: field name → messages. */
  errors?: Record<string, string[]>;

  /** Lightweight backend error message. */
  error?: string;

  /** Additional common message property used by some APIs. */
  message?: string;
}

/** Thrown by the API client for any non-2xx response. */
export class ApiError extends Error {
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    const message =
      problem.error ||
      problem.detail ||
      problem.message ||
      problem.title ||
      `Request failed with status ${ problem.status }.`;

    super(message);

    this.name = 'ApiError';
    this.problem = problem;
  }

  get status(): number {
    return this.problem.status;
  }

  /** Stable machine code for branching without parsing messages. */
  get code(): string | undefined {
    return this.problem.code;
  }

  get traceId(): string | undefined {
    return this.problem.traceId;
  }

  get fieldErrors(): Record<string, string[]> | undefined {
    return this.problem.errors;
  }

  get backendMessage(): string {
    return (
      this.problem.error ||
      this.problem.detail ||
      this.problem.message ||
      this.problem.title ||
      this.message
    );
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

