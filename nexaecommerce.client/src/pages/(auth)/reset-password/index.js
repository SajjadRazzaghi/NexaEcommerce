import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { CircleCheck, Loader2, TriangleAlert } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';
export default function ResetPasswordPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const [params] = useSearchParams();
    const email = params.get('email');
    const token = params.get('token');
    const [banner, setBanner] = useState(null);
    const schema = useMemo(() => z
        .object({
        newPassword: z.string().min(8, t('auth.valid.passwordMin')).max(128),
        confirmPassword: z.string().min(1, t('auth.valid.confirmRequired')),
    })
        .refine((v) => v.newPassword === v.confirmPassword, {
        message: t('auth.valid.mismatch'),
        path: ['confirmPassword'],
    }), [t]);
    const form = useForm({
        resolver: zodResolver(schema),
        defaultValues: { newPassword: '', confirmPassword: '' },
    });
    const reset = useMutation({
        mutationFn: authApi.resetPassword,
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['newPassword'])),
    });
    if (!email || !token) {
        return _jsx(InvalidLink, { children: t('auth.reset.invalidMissing') });
    }
    if (reset.isSuccess) {
        return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: "bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full", children: _jsx(CircleCheck, { className: "size-6" }) }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.reset.doneTitle') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.reset.doneDesc') }), _jsx(Button, { asChild: true, className: "mt-2", children: _jsx(Link, { to: "/login", children: t('auth.reset.goSignIn') }) })] }));
    }
    const onSubmit = form.handleSubmit((values) => {
        setBanner(null);
        reset.mutate({ email, token, newPassword: values.newPassword });
    });
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("header", { className: "grid gap-1.5", children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.reset.title') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.reset.subtitle', { email }) })] }), _jsx(FormBanner, { state: banner }), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: onSubmit, className: "grid gap-4", noValidate: true, children: [_jsx(FormField, { control: form.control, name: "newPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.reset.new') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", autoFocus: true, placeholder: t('auth.passwordPlaceholder'), ...field }) }), _jsx(FormDescription, { children: t('auth.reset.passwordHint') }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "confirmPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.reset.confirm') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", placeholder: t('auth.passwordPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsxs(Button, { type: "submit", disabled: reset.isPending, children: [reset.isPending && _jsx(Loader2, { className: "animate-spin" }), t('auth.reset.submit')] })] }) })] }));
}
function InvalidLink({ children }) {
    const { t } = useTranslation();
    return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: "bg-destructive/10 text-destructive mx-auto grid size-12 place-items-center rounded-full", children: _jsx(TriangleAlert, { className: "size-6" }) }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.reset.invalidTitle') }), _jsx("p", { className: "text-muted-foreground text-sm text-balance", children: children }), _jsx(Button, { asChild: true, variant: "outline", className: "mt-2", children: _jsx(Link, { to: "/forgot-password", children: t('auth.reset.requestNew') }) })] }));
}
