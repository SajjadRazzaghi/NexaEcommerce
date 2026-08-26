import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import { History, KeyRound, Lock, LockOpen, MailCheck, MailPlus, MoreHorizontal, Pencil, ShieldOff, Trash2, UserCog, UserPlus, Users as UsersIcon } from 'lucide-react';
import { toast } from 'sonner';
import { usersApi, PERM } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, } from '@/components/ui/dropdown-menu';
import { PageHeader } from '@/components/data-states';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { DataGrid, useDataGrid, selectColumn, DateCell } from '@/components/data-grid';
import { UserRolesDialog } from '@/components/admin/user-roles-dialog';
import { CreateUserDialog } from '@/components/admin/create-user-dialog';
import { EditUserDialog } from '@/components/admin/edit-user-dialog';
import { meta } from './meta';
export default function UsersPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const canCreate = usePermission(PERM.usersCreate);
    const canUpdate = usePermission(PERM.usersUpdate);
    const canDelete = usePermission(PERM.usersDelete);
    const canReadRoles = usePermission(PERM.rolesRead);
    const canAudit = usePermission(PERM.auditRead);
    const showActions = canUpdate || canDelete;
    const grid = useDataGrid({
        endpoint: '/users',
        queryKey: ['admin', 'users'],
        defaultSort: { id: 'createdAt', desc: true },
    });
    const [rolesFor, setRolesFor] = useState(null);
    const [editing, setEditing] = useState(null);
    const [deleting, setDeleting] = useState(null);
    const [bulkDeleteIds, setBulkDeleteIds] = useState(null);
    const [createOpen, setCreateOpen] = useState(false);
    const invalidate = () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });
    const onError = (fallback) => (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : fallback);
    const lock = useMutation({
        mutationFn: (user) => (user.lockedOut ? usersApi.unlock(user.id) : usersApi.lock(user.id)),
        onSuccess: (updated) => {
            toast.success(updated.lockedOut ? t('users.locked') : t('users.unlocked'));
            invalidate();
        },
        onError: onError(t('users.lockError')),
    });
    const remove = useMutation({
        mutationFn: (user) => usersApi.remove(user.id),
        onSuccess: () => {
            toast.success(t('users.deleted'));
            invalidate();
            setDeleting(null);
        },
        onError: onError(t('users.deleteError')),
    });
    const confirmEmail = useMutation({
        mutationFn: (user) => usersApi.confirmEmail(user.id),
        onSuccess: () => {
            toast.success(t('users.verified'));
            invalidate();
        },
        onError: onError(t('users.verifyError')),
    });
    const sendReset = useMutation({
        mutationFn: (user) => usersApi.sendPasswordReset(user.id),
        onSuccess: () => toast.success(t('users.resetSent')),
        onError: onError(t('users.resetError')),
    });
    const resendConfirm = useMutation({
        mutationFn: (user) => usersApi.resendConfirmation(user.id),
        onSuccess: () => toast.success(t('users.confirmationSent')),
        onError: onError(t('users.resendError')),
    });
    const disable2fa = useMutation({
        mutationFn: (user) => usersApi.disableTwoFactor(user.id),
        onSuccess: () => {
            toast.success(t('users.twoFactorDisabled'));
            invalidate();
        },
        onError: onError(t('users.disable2faError')),
    });
    const bulkLock = useMutation({
        mutationFn: (ids) => Promise.all(ids.map((id) => usersApi.lock(id))),
        onSuccess: (r) => {
            toast.success(t('users.bulkLocked', { count: r.length }));
            invalidate();
        },
        onError: onError(t('users.bulkLockError')),
    });
    const bulkRemove = useMutation({
        mutationFn: (ids) => Promise.all(ids.map((id) => usersApi.remove(id))),
        onSuccess: (r) => {
            toast.success(t('users.bulkDeleted', { count: r.length }));
            invalidate();
            setBulkDeleteIds(null);
        },
        onError: onError(t('users.bulkDeleteError')),
    });
    const rowActions = (user) => showActions && !user.isSelf ? (_jsx("div", { className: "text-end", children: _jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsx(Button, { variant: "ghost", size: "icon", className: "size-8", "aria-label": t('users.actionsFor', { email: user.email }), children: _jsx(MoreHorizontal, { className: "size-4" }) }) }), _jsxs(DropdownMenuContent, { align: "end", children: [canUpdate && (_jsxs(DropdownMenuItem, { onClick: () => setEditing(user), children: [_jsx(Pencil, {}), t('users.editUser')] })), canUpdate && canReadRoles && (_jsxs(DropdownMenuItem, { onClick: () => setRolesFor(user), children: [_jsx(UserCog, {}), t('users.editRoles')] })), canUpdate && !user.emailConfirmed && (_jsxs(DropdownMenuItem, { onClick: () => confirmEmail.mutate(user), children: [_jsx(MailCheck, {}), t('users.verify')] })), canUpdate && !user.emailConfirmed && (_jsxs(DropdownMenuItem, { onClick: () => resendConfirm.mutate(user), children: [_jsx(MailPlus, {}), t('users.resendConfirm')] })), canUpdate && (_jsxs(DropdownMenuItem, { onClick: () => sendReset.mutate(user), children: [_jsx(KeyRound, {}), t('users.sendReset')] })), canUpdate && user.twoFactorEnabled && (_jsxs(DropdownMenuItem, { onClick: () => disable2fa.mutate(user), children: [_jsx(ShieldOff, {}), t('users.disable2fa')] })), canAudit && (
                        // "AppUser" is the entity type the audit interceptor records for user rows (the CLR name);
                        // it's the coordinate the /audit/entity view keys on.
                        _jsxs(DropdownMenuItem, { onClick: () => navigate(`/audit/entity/AppUser/${user.id}`), children: [_jsx(History, {}), t('users.viewActivity')] })), canUpdate && (_jsxs(DropdownMenuItem, { onClick: () => lock.mutate(user), children: [user.lockedOut ? _jsx(LockOpen, {}) : _jsx(Lock, {}), user.lockedOut ? t('users.unlock') : t('users.lock')] })), canDelete && (_jsxs(DropdownMenuItem, { variant: "destructive", onClick: () => setDeleting(user), children: [_jsx(Trash2, {}), t('actions.delete')] }))] })] }) })) : null;
    // Inline column defs — ids are stable so the grid's column-visibility/sort state persists.
    const columns = [
        ...(showActions ? [selectColumn()] : []),
        {
            id: 'displayName',
            accessorKey: 'displayName',
            header: t('users.user'),
            meta: { label: t('users.user') },
            cell: ({ row }) => _jsx(UserIdentity, { user: row.original }),
        },
        {
            id: 'roles',
            header: t('users.roles'),
            enableSorting: false,
            meta: { label: t('users.roles') },
            cell: ({ row }) => _jsx(RoleBadges, { roles: row.original.roles }),
        },
        {
            id: 'status',
            header: t('fields.status'),
            enableSorting: false,
            meta: { label: t('fields.status') },
            cell: ({ row }) => _jsx(StatusBadges, { user: row.original }),
        },
        {
            id: 'createdAt',
            accessorKey: 'createdAt',
            header: t('fields.joined'),
            meta: { label: t('fields.joined') },
            cell: ({ row }) => _jsx(DateCell, { value: row.original.createdAt }),
        },
        ...(showActions
            ? [{ id: '__actions', header: '', enableSorting: false, enableHiding: false, meta: { label: '' }, cell: ({ row }) => rowActions(row.original) }]
            : []),
    ];
    return (_jsxs("div", { className: "grid gap-4", children: [_jsx(PageHeader, { title: t('nav.users'), description: t('pages.usersDesc'), actions: canCreate ? (_jsxs(Button, { onClick: () => setCreateOpen(true), children: [_jsx(UserPlus, { className: "size-4" }), t('users.newUser')] })) : undefined }), _jsx(DataGrid, { grid: grid, columns: columns, getRowId: (u) => u.id, searchPlaceholder: t('users.searchPlaceholder'), viewKey: "users", exportable: true, empty: {
                    icon: UsersIcon,
                    title: t('users.emptyTitle'),
                    description: t('users.emptyDesc'),
                }, bulkActions: showActions
                    ? (ids, clear) => {
                        const targets = grid.items.filter((u) => ids.includes(u.id) && !u.isSelf).map((u) => u.id);
                        return (_jsxs(_Fragment, { children: [canUpdate && (_jsxs(Button, { variant: "outline", size: "sm", disabled: targets.length === 0 || bulkLock.isPending, onClick: () => {
                                        bulkLock.mutate(targets);
                                        clear();
                                    }, children: [_jsx(Lock, { className: "size-4" }), t('users.lock')] })), canDelete && (_jsxs(Button, { variant: "outline", size: "sm", disabled: targets.length === 0, onClick: () => setBulkDeleteIds(targets), children: [_jsx(Trash2, { className: "size-4" }), t('actions.delete')] }))] }));
                    }
                    : undefined }), _jsx(CreateUserDialog, { open: createOpen, onOpenChange: setCreateOpen }), _jsx(EditUserDialog, { open: !!editing, onOpenChange: (open) => !open && setEditing(null), user: editing }), _jsx(UserRolesDialog, { open: !!rolesFor, onOpenChange: (open) => !open && setRolesFor(null), user: rolesFor }), _jsx(ConfirmDialog, { open: !!deleting, onOpenChange: (open) => !open && setDeleting(null), title: t('users.deleteTitle', { name: deleting?.displayName ?? deleting?.email }), description: t('users.deleteDesc'), confirmLabel: t('users.deleteConfirm'), destructive: true, pending: remove.isPending, onConfirm: () => deleting && remove.mutate(deleting) }), _jsx(ConfirmDialog, { open: !!bulkDeleteIds, onOpenChange: (open) => !open && setBulkDeleteIds(null), title: t('users.bulkDeleteTitle', { count: bulkDeleteIds?.length ?? 0 }), description: t('users.bulkDeleteDesc'), confirmLabel: t('users.bulkDeleteConfirm'), destructive: true, pending: bulkRemove.isPending, onConfirm: () => bulkDeleteIds && bulkRemove.mutate(bulkDeleteIds) })] }));
}
function UserIdentity({ user }) {
    const { t } = useTranslation();
    return (_jsxs("div", { className: "flex items-center gap-3", children: [_jsx(Avatar, { className: "size-9", children: _jsx(AvatarFallback, { children: initials(user.displayName ?? user.email) }) }), _jsxs("div", { className: "min-w-0", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("span", { className: "truncate font-medium", children: user.displayName ?? user.email }), user.isSelf && _jsx(Badge, { variant: "outline", children: t('users.you') })] }), _jsx("p", { className: "text-muted-foreground truncate text-sm", children: user.email })] })] }));
}
function RoleBadges({ roles }) {
    if (roles.length === 0)
        return _jsx("span", { className: "text-muted-foreground text-sm", children: "\u2014" });
    return (_jsx("div", { className: "flex flex-wrap gap-1", children: roles.map((role) => (_jsx(Badge, { variant: "secondary", children: role }, role))) }));
}
function StatusBadges({ user }) {
    const { t } = useTranslation();
    return (_jsxs("div", { className: "flex flex-wrap gap-1", children: [user.lockedOut && _jsx(Badge, { variant: "destructive", children: t('users.lockedBadge') }), !user.emailConfirmed && _jsx(Badge, { variant: "secondary", children: t('users.pendingEmail') }), user.twoFactorEnabled && _jsx(Badge, { variant: "success", children: t('users.twoFactor') }), user.emailConfirmed && !user.lockedOut && !user.twoFactorEnabled && (_jsx("span", { className: "text-muted-foreground text-sm", children: t('users.activeStatus') }))] }));
}
function initials(value) {
    const parts = value.trim().split(/\s+/);
    if (parts.length >= 2)
        return (parts[0][0] + parts[1][0]).toUpperCase();
    return value.slice(0, 2).toUpperCase();
}
