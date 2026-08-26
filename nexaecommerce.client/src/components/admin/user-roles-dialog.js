import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
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
import { Skeleton } from '@/components/ui/skeleton';
import { ErrorState } from '@/components/data-states';
/** Assign roles to a user. The inner form is keyed by user id so it remounts with the user's current
 * roles as initial state (no effect sync); roles are loaded lazily while the dialog is open. */
export function UserRolesDialog({ open, onOpenChange, user, }) {
    const { t } = useTranslation();
    return (_jsx(Dialog, { open: open, onOpenChange: onOpenChange, children: _jsxs(DialogContent, { className: "sm:max-w-md", children: [_jsxs(DialogHeader, { children: [_jsx(DialogTitle, { children: t('users.editRoles') }), _jsx(DialogDescription, { children: user?.displayName ?? user?.email })] }), user && _jsx(UserRolesForm, { user: user, onClose: () => onOpenChange(false) }, user.id)] }) }));
}
function UserRolesForm({ user, onClose }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });
    const [selected, setSelected] = useState(() => new Set(user.roles));
    const save = useMutation({
        mutationFn: (roleNames) => usersApi.updateRoles(user.id, roleNames),
        onSuccess: () => {
            toast.success(t('users.rolesUpdated'));
            queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
            onClose();
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('users.rolesUpdateError')),
    });
    const toggle = (name, on) => setSelected((prev) => {
        const next = new Set(prev);
        if (on)
            next.add(name);
        else
            next.delete(name);
        return next;
    });
    return (_jsxs(_Fragment, { children: [roles.isLoading ? (_jsx("div", { className: "grid gap-2 py-2", children: [0, 1, 2].map((i) => (_jsx(Skeleton, { className: "h-12 w-full rounded-lg" }, i))) })) : roles.isError ? (_jsx(ErrorState, { error: roles.error, onRetry: () => roles.refetch(), retrying: roles.isFetching, message: t('users.rolesLoadError') })) : (_jsx("ul", { className: "grid max-h-[50svh] gap-1 overflow-y-auto py-1", children: roles.data?.map((role) => (_jsx("li", { children: _jsxs("label", { className: "hover:bg-muted/40 flex cursor-pointer items-start gap-3 rounded-lg p-2.5", children: [_jsx(Checkbox, { checked: selected.has(role.name), onCheckedChange: (v) => toggle(role.name, v === true), className: "mt-0.5" }), _jsxs("span", { className: "grid gap-0.5", children: [_jsx("span", { className: "font-medium leading-none", children: role.name }), _jsx("span", { className: "text-muted-foreground text-xs", children: role.permissions.includes('*')
                                            ? t('roles.allPermissions')
                                            : t('roles.permissionCount', { count: role.permissions.length }) })] })] }) }, role.id))) })), _jsxs(DialogFooter, { children: [_jsx(Button, { variant: "outline", onClick: onClose, disabled: save.isPending, children: t('common.cancel') }), _jsxs(Button, { onClick: () => save.mutate([...selected]), disabled: save.isPending || roles.isLoading, children: [save.isPending && _jsx(Loader2, { className: "animate-spin" }), t('users.saveRoles')] })] })] }));
}
