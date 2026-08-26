import { isApiError } from '@/lib/problem';
/**
 * Routes a ProblemDetails error from a mutation onto the form: field-level messages go inline
 * (via setError), and anything left over — non-field validation, INVALID_CREDENTIALS, 500s —
 * becomes the top-of-form banner. Returns the banner state (message + traceId for the fold-out).
 */
export function applyApiErrorToForm(error, setError, knownFields) {
    if (!isApiError(error)) {
        return { message: 'Something went wrong. Please try again.' };
    }
    const fields = error.fieldErrors;
    if (!fields || Object.keys(fields).length === 0) {
        return { message: error.problem.detail ?? error.message, traceId: error.traceId };
    }
    const leftovers = [];
    for (const [field, messages] of Object.entries(fields)) {
        if (field && knownFields.includes(field)) {
            setError(field, { message: messages.join(' ') });
        }
        else {
            leftovers.push(...messages);
        }
    }
    return { message: leftovers.length > 0 ? leftovers.join(' ') : null, traceId: error.traceId };
}
