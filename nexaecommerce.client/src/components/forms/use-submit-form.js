import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
/**
 * Standardizes the submit flow (§7.2): clears the banner, runs the mutation, toasts on success, and
 * routes a `ProblemDetails` error back onto the form — field messages inline, the rest into the
 * banner. Returns `submit` (an RHF-validated handler) plus `isPending` and `banner` for the UI.
 */
export function useSubmitForm({ form, mutationFn, fields, successMessage, onSuccess, transform, }) {
    const [banner, setBanner] = useState(null);
    const mutation = useMutation({
        mutationFn,
        onSuccess: (result) => {
            if (successMessage)
                toast.success(successMessage);
            onSuccess?.(result);
        },
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, fields ?? Object.keys(form.getValues()))),
    });
    const submit = form.handleSubmit((values) => {
        setBanner(null);
        mutation.mutate(transform ? transform(values) : values);
    });
    return { submit, isPending: mutation.isPending, banner, setBanner, mutation };
}
