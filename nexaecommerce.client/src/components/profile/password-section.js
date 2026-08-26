import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
import { authKeys, useAuth } from '@/hooks/use-auth';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
export function PasswordSection() {
    const { t } = useTranslation();
    const { user } = useAuth();
    // OAuth-only accounts have no password yet — show "set" (no current-password field) instead of "change".
    const hasPassword = user?.hasPassword ?? true;
    const queryClient = useQueryClient();
    const [banner, setBanner] = useState(null);
    const schema = useMemo(() => z
        .object({
        currentPassword: z.string(),
        newPassword: z.string().min(8, t('profile.password.min')).max(128),
        confirmPassword: z.string().min(1, t('profile.password.confirmRequired')),
    })
        .refine((v) => v.newPassword === v.confirmPassword, {
        message: t('profile.password.mismatch'),
        path: ['confirmPassword'],
    }), [t]);
    const form = useForm({
        resolver: zodResolver(schema),
        defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
    });
    const change = useMutation({
        mutationFn: authApi.changePassword,
        onSuccess: () => {
            toast.success(hasPassword ? t('profile.password.changed') : t('profile.password.set'));
            form.reset();
            queryClient.invalidateQueries({ queryKey: authKeys.me }); // refresh hasPassword so this card flips to "change"
        },
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['currentPassword', 'newPassword'])),
    });
    const onSubmit = form.handleSubmit((values) => {
        if (hasPassword && !values.currentPassword) {
            form.setError('currentPassword', { message: t('profile.password.enterCurrent') });
            return;
        }
        setBanner(null);
        change.mutate({ currentPassword: values.currentPassword || undefined, newPassword: values.newPassword });
    });
    return (_jsxs(Card, { children: [_jsxs(CardHeader, { children: [_jsx(CardTitle, { children: t('profile.sections.password') }), _jsx(CardDescription, { children: hasPassword ? t('profile.password.changeDesc') : t('profile.password.setDesc') })] }), _jsxs(CardContent, { children: [_jsx(FormBanner, { state: banner }), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: onSubmit, className: "mt-4 grid max-w-sm gap-4", children: [hasPassword && (_jsx(FormField, { control: form.control, name: "currentPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('profile.password.current') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "current-password", ...field }) }), _jsx(FormMessage, {})] })) })), _jsx(FormField, { control: form.control, name: "newPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('profile.password.new') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", ...field }) }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "confirmPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('profile.password.confirm') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", ...field }) }), _jsx(FormMessage, {})] })) }), _jsx("div", { children: _jsxs(Button, { type: "submit", disabled: change.isPending, children: [change.isPending && _jsx(Loader2, { className: "animate-spin" }), hasPassword ? t('profile.password.update') : t('profile.password.setBtn')] }) })] }) })] })] }));
}
