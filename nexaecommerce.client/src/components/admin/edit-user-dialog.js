import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { usersApi } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
/** Edit a user's display name and email. Keyed by user id so it remounts with the user's current values
 * as initial state (the documented derive-state-on-mount pattern — no effect sync). */
export function EditUserDialog({ open, onOpenChange, user, }) {
    const { t } = useTranslation();
    return (_jsx(Dialog, { open: open, onOpenChange: onOpenChange, children: _jsxs(DialogContent, { className: "sm:max-w-md", children: [_jsxs(DialogHeader, { children: [_jsx(DialogTitle, { children: t('users.editUser') }), _jsx(DialogDescription, { children: t('users.editUserDesc') })] }), user && _jsx(EditUserForm, { user: user, onClose: () => onOpenChange(false) }, user.id)] }) }));
}
function EditUserForm({ user, onClose }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const [displayName, setDisplayName] = useState(user.displayName ?? '');
    const [email, setEmail] = useState(user.email);
    const save = useMutation({
        mutationFn: (body) => usersApi.update(user.id, body),
        onSuccess: () => {
            toast.success(t('users.updated'));
            queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
            onClose();
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.updateError')),
    });
    const submit = (e) => {
        e.preventDefault();
        save.mutate({ displayName: displayName.trim() || null, email: email.trim() });
    };
    return (_jsxs("form", { onSubmit: submit, className: "grid gap-4", children: [_jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "eu-name", children: t('users.displayNameOptional') }), _jsx(Input, { id: "eu-name", value: displayName, onChange: (e) => setDisplayName(e.target.value) })] }), _jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "eu-email", children: t('users.emailLabel') }), _jsx(Input, { id: "eu-email", type: "email", required: true, value: email, onChange: (e) => setEmail(e.target.value) })] }), _jsxs(DialogFooter, { children: [_jsx(Button, { type: "button", variant: "outline", onClick: onClose, disabled: save.isPending, children: t('common.cancel') }), _jsxs(Button, { type: "submit", disabled: save.isPending || !email.trim(), children: [save.isPending && _jsx(Loader2, { className: "animate-spin" }), t('users.saveUser')] })] })] }));
}
