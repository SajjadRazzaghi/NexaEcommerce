import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { ArrowLeft, Loader2, MailCheck } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';
export default function ForgotPasswordPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const [banner, setBanner] = useState(null);
    const schema = useMemo(() => z.object({ email: z.string().min(1, t('auth.valid.emailRequired')).email(t('auth.valid.emailInvalid')) }), [t]);
    const form = useForm({ resolver: zodResolver(schema), defaultValues: { email: '' } });
    const forgot = useMutation({
        mutationFn: authApi.forgotPassword,
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['email'])),
    });
    const onSubmit = form.handleSubmit((values) => {
        setBanner(null);
        forgot.mutate(values);
    });
    if (forgot.isSuccess) {
        return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: "bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full", children: _jsx(MailCheck, { className: "size-6" }) }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.forgot.sentTitle') }), _jsx("p", { className: "text-muted-foreground text-sm text-balance", children: t('auth.forgot.sentDesc', { email: form.getValues('email') }) }), _jsx(Button, { asChild: true, variant: "outline", className: "mt-2", children: _jsx(Link, { to: "/login", children: t('auth.backToSignIn') }) })] }));
    }
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("header", { className: "grid gap-1.5", children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.forgot.title') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.forgot.subtitle') })] }), _jsx(FormBanner, { state: banner }), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: onSubmit, className: "grid gap-4", noValidate: true, children: [_jsx(FormField, { control: form.control, name: "email", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.email') }), _jsx(FormControl, { children: _jsx(Input, { type: "email", autoComplete: "email", autoFocus: true, placeholder: t('auth.emailPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsxs(Button, { type: "submit", disabled: forgot.isPending, children: [forgot.isPending && _jsx(Loader2, { className: "animate-spin" }), t('auth.forgot.submit')] })] }) }), _jsxs(Link, { to: "/login", className: "text-muted-foreground hover:text-foreground inline-flex items-center justify-center gap-1.5 text-sm", children: [_jsx(ArrowLeft, { className: "size-4" }), t('auth.backToSignIn')] })] }));
}
