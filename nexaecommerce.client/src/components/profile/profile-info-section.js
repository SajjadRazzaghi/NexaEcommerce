import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { BadgeCheck, Loader2 } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { useAuth, useSetCurrentUser } from '@/hooks/use-auth';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Form } from '@/components/ui/form';
import { FormBanner } from '@/components/auth/form-banner';
import { Field, useSubmitForm } from '@/components/forms';
const schema = z.object({ displayName: z.string().max(100) });
export function ProfileInfoSection() {
    const { t } = useTranslation();
    const { user } = useAuth();
    const setCurrentUser = useSetCurrentUser();
    const form = useForm({
        resolver: zodResolver(schema),
        values: { displayName: user?.displayName ?? '' },
    });
    const { submit, isPending, banner } = useSubmitForm({
        form,
        mutationFn: authApi.updateProfile,
        fields: ['displayName'],
        successMessage: t('profile.info.updated'),
        onSuccess: setCurrentUser,
        transform: (values) => ({ displayName: values.displayName.trim() || null }),
    });
    if (!user)
        return null;
    // Uploadable avatar in editions with FileUploads; a static initials avatar otherwise. Built in TS so
    // the JSX below needs no build-time conditional.
    const slots = {
        avatarBlock: (_jsxs(Avatar, { className: "size-16", children: [_jsx(AvatarImage, { src: user.avatarUrl ?? undefined, alt: "" }), _jsx(AvatarFallback, { className: "text-lg", children: initials(user.displayName ?? user.email) })] })),
    };
    return (_jsxs(Card, { children: [_jsxs(CardHeader, { children: [_jsx(CardTitle, { children: t('profile.sections.profile') }), _jsx(CardDescription, { children: t('profile.info.desc') })] }), _jsxs(CardContent, { className: "grid gap-6", children: [_jsxs("div", { className: "flex flex-wrap items-center gap-4", children: [slots.avatarBlock, _jsxs("div", { className: "grid gap-1", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("span", { className: "font-medium", children: user.email }), user.emailConfirmed ? (_jsxs(Badge, { variant: "success", children: [_jsx(BadgeCheck, {}), t('profile.info.verified')] })) : (_jsx(Badge, { variant: "secondary", children: t('profile.info.unverified') }))] }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('profile.info.emailIdentity') })] })] }), _jsx(FormBanner, { state: banner }), _jsx(Form, { ...form, children: _jsxs("form", { onSubmit: submit, className: "grid max-w-sm gap-4", children: [_jsx(Field, { name: "displayName", label: t('profile.info.displayName'), placeholder: t('profile.info.namePlaceholder') }), _jsx("div", { children: _jsxs(Button, { type: "submit", disabled: isPending || !form.formState.isDirty, children: [isPending && _jsx(Loader2, { className: "animate-spin" }), t('common.saveChanges')] }) })] }) })] })] }));
}
function initials(value) {
    const parts = value.trim().split(/\s+/);
    if (parts.length >= 2)
        return (parts[0][0] + parts[1][0]).toUpperCase();
    return value.slice(0, 2).toUpperCase();
}
