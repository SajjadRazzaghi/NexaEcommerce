import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2, MailCheck, Lock } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePublicConfig } from '@/hooks/use-public-config';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';
export default function RegisterPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const [banner, setBanner] = useState(null);
    const { data: publicConfig } = usePublicConfig();
    const schema = useMemo(() => z
        .object({
        displayName: z.string().max(100).optional(),
        email: z.string().min(1, t('auth.valid.emailRequired')).email(t('auth.valid.emailInvalid')),
        password: z.string().min(8, t('auth.valid.passwordMin')).max(128),
        confirmPassword: z.string().min(1, t('auth.valid.confirmRequired')),
    })
        .refine((v) => v.password === v.confirmPassword, {
        message: t('auth.valid.mismatch'),
        path: ['confirmPassword'],
    }), [t]);
    const form = useForm({
        resolver: zodResolver(schema),
        defaultValues: { displayName: '', email: '', password: '', confirmPassword: '' },
    });
    const register = useMutation({
        mutationFn: authApi.register,
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['email', 'password'])),
    });
    const onSubmit = form.handleSubmit((values) => {
        setBanner(null);
        register.mutate({ email: values.email, password: values.password, displayName: values.displayName || undefined });
    });
    if (publicConfig && !publicConfig.allowRegistration) {
        return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: "bg-muted text-muted-foreground mx-auto grid size-12 place-items-center rounded-full", children: _jsx(Lock, { className: "size-6" }) }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.register.closedTitle') }), _jsx("p", { className: "text-muted-foreground text-sm text-balance", children: t('auth.register.closedDesc') }), _jsx(Button, { asChild: true, variant: "outline", className: "mt-2", children: _jsx(Link, { to: "/login", children: t('auth.backToSignIn') }) })] }));
    }
    if (register.isSuccess) {
        return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: "bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full", children: _jsx(MailCheck, { className: "size-6" }) }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.register.checkEmailTitle') }), _jsx("p", { className: "text-muted-foreground text-sm text-balance", children: t('auth.register.checkEmailDesc', { email: form.getValues('email') }) }), _jsx(Button, { asChild: true, variant: "outline", className: "mt-2", children: _jsx(Link, { to: "/login", children: t('auth.backToSignIn') }) })] }));
    }
    // Optional OAuth sign-in buttons — built in TS into a const slot so the JSX below needs no build-time conditional.
    const slots = {};
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("header", { className: "grid gap-1.5", children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.register.title') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.register.subtitle') })] }), _jsx(FormBanner, { state: banner }), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: onSubmit, className: "grid gap-4", noValidate: true, children: [_jsx(FormField, { control: form.control, name: "displayName", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.register.name') }), _jsx(FormControl, { children: _jsx(Input, { autoComplete: "name", placeholder: t('auth.register.namePlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "email", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.email') }), _jsx(FormControl, { children: _jsx(Input, { type: "email", autoComplete: "email", placeholder: t('auth.emailPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "password", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.password') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", placeholder: t('auth.passwordPlaceholder'), ...field }) }), _jsx(FormDescription, { children: t('auth.register.passwordHint') }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "confirmPassword", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.register.confirm') }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "new-password", placeholder: t('auth.passwordPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsxs(Button, { type: "submit", disabled: register.isPending, children: [register.isPending && _jsx(Loader2, { className: "animate-spin" }), t('auth.register.submit')] })] }) }), slots.oauthButtons, _jsxs("p", { className: "text-muted-foreground text-center text-sm", children: [t('auth.register.haveAccount'), ' ', _jsx(Link, { to: "/login", className: "text-foreground font-medium hover:underline", children: t('auth.register.signIn') })] })] }));
}
