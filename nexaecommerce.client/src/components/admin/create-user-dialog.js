import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { rolesApi, usersApi } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Skeleton } from '@/components/ui/skeleton';
/** Provision a new user. Invite-first: with "send invitation" on, the account is created and emailed a
 * "set your password" link; turning it off reveals a temporary-password field for offline provisioning.
 * The form is mounted only while open so it always starts from defaults (no effect-based reset). */
export function CreateUserDialog({ open, onOpenChange }) {
    const { t } = useTranslation();
    return (_jsx(Dialog, { open: open, onOpenChange: onOpenChange, children: _jsxs(DialogContent, { className: "sm:max-w-md", children: [_jsxs(DialogHeader, { children: [_jsx(DialogTitle, { children: t('users.newUser') }), _jsx(DialogDescription, { children: t('users.createDesc') })] }), open && _jsx(CreateUserForm, { onClose: () => onOpenChange(false) })] }) }));
}
function CreateUserForm({ onClose }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });
    const [email, setEmail] = useState('');
    const [displayName, setDisplayName] = useState('');
    const [selectedRoles, setSelectedRoles] = useState(() => new Set());
    const [emailConfirmed, setEmailConfirmed] = useState(true);
    const [sendInvite, setSendInvite] = useState(true);
    const [password, setPassword] = useState('');
    const create = useMutation({
        mutationFn: (body) => usersApi.create(body),
        onSuccess: (_user, body) => {
            toast.success(body.sendInvite && !body.password ? t('users.invitedToast') : t('users.createdToast'));
            queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
            onClose();
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.createError')),
    });
    const toggleRole = (name, on) => setSelectedRoles((prev) => {
        const next = new Set(prev);
        if (on)
            next.add(name);
        else
            next.delete(name);
        return next;
    });
    const submit = (e) => {
        e.preventDefault();
        create.mutate({
            email: email.trim(),
            displayName: displayName.trim() || null,
            roles: [...selectedRoles],
            emailConfirmed,
            sendInvite,
            password: sendInvite ? null : password || null,
        });
    };
    return (_jsxs("form", { onSubmit: submit, className: "grid gap-4", children: [_jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "cu-email", children: t('users.emailLabel') }), _jsx(Input, { id: "cu-email", type: "email", required: true, autoFocus: true, value: email, onChange: (e) => setEmail(e.target.value) })] }), _jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "cu-name", children: t('users.displayNameOptional') }), _jsx(Input, { id: "cu-name", value: displayName, onChange: (e) => setDisplayName(e.target.value) })] }), _jsxs("div", { className: "grid gap-1.5", children: [_jsx(Label, { children: t('users.rolesOptional') }), roles.isLoading ? (_jsx(Skeleton, { className: "h-20 w-full rounded-lg" })) : (_jsx("ul", { className: "max-h-40 overflow-y-auto rounded-lg border p-1", children: roles.data?.map((role) => (_jsx("li", { children: _jsxs("label", { className: "hover:bg-muted/40 flex cursor-pointer items-center gap-2 rounded-md p-2 text-sm", children: [_jsx(Checkbox, { checked: selectedRoles.has(role.name), onCheckedChange: (v) => toggleRole(role.name, v === true) }), role.name] }) }, role.id))) })), _jsx("p", { className: "text-muted-foreground text-xs", children: t('users.rolesHint') })] }), _jsxs("label", { className: "flex items-center justify-between gap-3", children: [_jsx("span", { className: "text-sm", children: t('users.markVerified') }), _jsx(Switch, { checked: emailConfirmed, onCheckedChange: setEmailConfirmed })] }), _jsxs("label", { className: "flex items-center justify-between gap-3", children: [_jsx("span", { className: "text-sm", children: t('users.sendInvite') }), _jsx(Switch, { checked: sendInvite, onCheckedChange: setSendInvite })] }), !sendInvite && (_jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "cu-pass", children: t('users.tempPassword') }), _jsx(Input, { id: "cu-pass", type: "text", value: password, onChange: (e) => setPassword(e.target.value), autoComplete: "new-password" }), _jsx("p", { className: "text-muted-foreground text-xs", children: t('users.tempPasswordHint') })] })), _jsxs(DialogFooter, { children: [_jsx(Button, { type: "button", variant: "outline", onClick: onClose, disabled: create.isPending, children: t('common.cancel') }), _jsxs(Button, { type: "submit", disabled: create.isPending || !email.trim(), children: [create.isPending && _jsx(Loader2, { className: "animate-spin" }), t('users.create')] })] })] }));
}
