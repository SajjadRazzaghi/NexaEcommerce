// src/components/forms/use-submit-form.tsx
import { useState } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import { toast } from 'sonner';
import type { FormBannerState } from '@/components/auth/form-banner';

interface UseSubmitFormOptions<TFormValues, TApiData, TResponse> {
    form: UseFormReturn<TFormValues>;
    mutationFn: (data: TApiData) => Promise<TResponse>;
    fields: (keyof TFormValues)[];
    successMessage?: string;
    onSuccess?: (data: TResponse) => void;
    onError?: (error: unknown) => void;
    transform: (values: TFormValues) => TApiData;
}

export function useSubmitForm<TFormValues, TApiData, TResponse>({
    form,
    mutationFn,
    fields,
    successMessage,
    onSuccess,
    onError,
    transform,
}: UseSubmitFormOptions<TFormValues, TApiData, TResponse>) {
    const [isPending, setIsPending] = useState(false);
    const [banner, setBanner] = useState<FormBannerState | null>(null);

    const submit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setBanner(null);

        // Trigger validation for all fields
        const result = await form.trigger(fields as any, { shouldFocus: true });

        if (!result) {
            // Scroll to first error
            const firstError = document.querySelector('[data-error="true"]');
            if (firstError) {
                firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
            return;
        }

        const values = form.getValues();
        setIsPending(true);

        try {
            const data = transform(values);
            const response = await mutationFn(data);

            if (successMessage) {
                toast.success(successMessage);
            }

            onSuccess?.(response);
        } catch (error: any) {
            // Handle API errors
            console.error('Form submission error:', error);

            if (error?.response?.data?.message) {
                setBanner({
                    message: error.response.data.message,
                    traceId: error.response.data.traceId,
                });
            } else if (error?.message) {
                setBanner({
                    message: error.message,
                });
            } else {
                setBanner({
                    message: 'An unexpected error occurred. Please try again.',
                });
            }

            onError?.(error);
        } finally {
            setIsPending(false);
        }
    };

    return {
        submit,
        isPending,
        banner,
    };
}