import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { Loader2, FlaskConical } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { applyApiErrorToForm } from '@/lib/api/form-errors';
import { isApiError } from '@/lib/problem';
import { useSetCurrentUser } from '@/hooks/use-auth';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { usePublicConfig } from '@/hooks/use-public-config';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Checkbox } from '@/components/ui/checkbox';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';
export default function LoginPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const navigate = useNavigate();
    const [params] = useSearchParams();
    const returnUrl = safeReturn(params.get('returnUrl'));
    const setCurrentUser = useSetCurrentUser();
    const schema = useMemo(() => z.object({
        email: z.string().min(1, t('auth.valid.emailRequired')).email(t('auth.valid.emailInvalid')),
        password: z.string().min(1, t('auth.valid.passwordRequired')),
        rememberMe: z.boolean(),
    }), [t]);
    const form = useForm({
        resolver: zodResolver(schema),
        defaultValues: { email: '', password: '', rememberMe: true },
    });
    const { data: publicConfig } = usePublicConfig();
    const oauthError = params.get('error');
    const [banner, setBanner] = useState(oauthError ? { message: t(`auth.oauthError.${oauthError}`, { defaultValue: t('auth.oauthError.generic') }) } : null);
    const login = useMutation({
        mutationFn: authApi.login,
        onSuccess: (result) => {
            if (result.user) {
                setCurrentUser(result.user);
                navigate(returnUrl, { replace: true });
            }
        },
        onError: (error) => setBanner(applyApiErrorToForm(error, form.setError, ['email', 'password'])),
    });
    const resend = useMutation({ mutationFn: authApi.resendConfirmation });
    const needsConfirmation = isApiError(login.error) && login.error.code === 'EMAIL_NOT_CONFIRMED';
    const onSubmit = form.handleSubmit((values) => {
        setBanner(null);
        login.mutate(values);
    });
    // Optional OAuth sign-in buttons — built in TS into a const slot so the JSX below needs no build-time conditional.
    const slots = {};
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("header", { className: "grid gap-1.5", children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('auth.login.title') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.login.subtitle') })] }), publicConfig?.demoLogin && (_jsxs("div", { className: "bg-muted/40 rounded-lg border px-3.5 py-3 text-sm", children: [_jsxs("div", { className: "text-foreground flex items-center gap-2 font-medium", children: [_jsx(FlaskConical, { className: "size-4" }), t('auth.login.demoTitle')] }), _jsxs("button", { type: "button", onClick: () => {
                            form.setValue('email', publicConfig?.demoLogin?.email ?? '', { shouldValidate: true });
                            form.setValue('password', publicConfig?.demoLogin?.password ?? '', { shouldValidate: true });
                        }, className: "bg-background hover:border-primary/50 mt-2 inline-flex items-center gap-2 rounded-md border px-2.5 py-1.5 font-mono text-xs transition-colors", children: [_jsx("span", { children: publicConfig.demoLogin.email }), _jsx("span", { className: "text-muted-foreground", children: "/" }), _jsx("span", { children: publicConfig.demoLogin.password })] }), _jsx("p", { className: "text-muted-foreground mt-2 text-xs", children: t('auth.login.demoHint') })] })), _jsx(FormBanner, { state: banner }), needsConfirmation && (_jsx(Button, { type: "button", variant: "outline", size: "sm", disabled: resend.isPending || resend.isSuccess, onClick: () => resend.mutate({ email: form.getValues('email') }), children: resend.isSuccess ? t('auth.login.resendSent') : t('auth.login.resend') })), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: onSubmit, className: "grid gap-4", noValidate: true, children: [_jsx(FormField, { control: form.control, name: "email", render: ({ field }) => (_jsxs(FormItem, { children: [_jsx(FormLabel, { children: t('auth.email') }), _jsx(FormControl, { children: _jsx(Input, { type: "email", autoComplete: "email", autoFocus: true, placeholder: t('auth.emailPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "password", render: ({ field }) => (_jsxs(FormItem, { children: [_jsxs("div", { className: "flex items-center justify-between", children: [_jsx(FormLabel, { children: t('auth.password') }), _jsx(Link, { to: "/forgot-password", className: "text-muted-foreground hover:text-foreground text-sm", children: t('auth.login.forgot') })] }), _jsx(FormControl, { children: _jsx(PasswordInput, { autoComplete: "current-password", placeholder: t('auth.passwordPlaceholder'), ...field }) }), _jsx(FormMessage, {})] })) }), _jsx(FormField, { control: form.control, name: "rememberMe", render: ({ field }) => (_jsxs(FormItem, { className: "flex items-center gap-2", children: [_jsx(FormControl, { children: _jsx(Checkbox, { checked: field.value, onCheckedChange: field.onChange, id: "rememberMe" }) }), _jsx(FormLabel, { htmlFor: "rememberMe", className: "font-normal", children: t('auth.login.keepSignedIn') })] })) }), _jsxs(Button, { type: "submit", disabled: login.isPending, children: [login.isPending && _jsx(Loader2, { className: "animate-spin" }), t('auth.login.signIn')] })] }) }), slots.oauthButtons, publicConfig?.allowRegistration !== false && (_jsxs("p", { className: "text-muted-foreground text-center text-sm", children: [t('auth.login.noAccount'), ' ', _jsx(Link, { to: "/register", className: "text-foreground font-medium hover:underline", children: t('auth.login.createOne') })] }))] }));
}
function safeReturn(value) {
    return value && value.startsWith('/') && !value.startsWith('//') ? value : '/';
}
