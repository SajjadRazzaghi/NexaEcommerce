import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Lock, Pencil, Plus, Shield, Trash2, Users } from 'lucide-react';
import { toast } from 'sonner';
import { rolesApi, permissionsApi, PERM } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { ConfirmDialog } from '@/components/confirm-dialog';
import { EmptyState, ErrorState } from '@/components/data-states';
import { RoleEditorDialog } from '@/components/admin/role-editor-dialog';
import { meta } from './meta';
export default function RolesPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const queryClient = useQueryClient();
    const canCreate = usePermission(PERM.rolesCreate);
    const canUpdate = usePermission(PERM.rolesUpdate);
    const canDelete = usePermission(PERM.rolesDelete);
    const roles = useQuery({ queryKey: ['admin', 'roles'], queryFn: rolesApi.list });
    const catalog = useQuery({ queryKey: ['admin', 'permissions'], queryFn: permissionsApi.catalog });
    const [editing, setEditing] = useState(null);
    const [editorOpen, setEditorOpen] = useState(false);
    const [deleting, setDeleting] = useState(null);
    const remove = useMutation({
        mutationFn: (role) => rolesApi.remove(role.id),
        onSuccess: () => {
            toast.success(t('roles.deleted'));
            queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] });
            setDeleting(null);
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('roles.deleteError')),
    });
    const openCreate = () => {
        setEditing(null);
        setEditorOpen(true);
    };
    const openEdit = (role) => {
        setEditing(role);
        setEditorOpen(true);
    };
    return (_jsxs("div", { className: "grid gap-4", children: [_jsxs("header", { children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('roles.title') }), _jsx("p", { className: "text-muted-foreground mt-1", children: t('roles.subtitle') })] }), _jsxs("div", { className: "flex items-center justify-between gap-3", children: [_jsx("p", { className: "text-muted-foreground text-sm", children: t('roles.bundleHint') }), canCreate && (_jsxs(Button, { size: "sm", onClick: openCreate, disabled: !catalog.data, children: [_jsx(Plus, {}), t('roles.newRole')] }))] }), _jsx(Card, { children: _jsx(CardContent, { className: "p-0", children: roles.isLoading ? (_jsx(LoadingRows, {})) : roles.isError ? (_jsx(ErrorState, { error: roles.error, onRetry: () => roles.refetch(), retrying: roles.isFetching, message: t('roles.loadError') })) : !roles.data || roles.data.length === 0 ? (_jsx(EmptyState, { icon: Shield, title: t('roles.emptyTitle'), description: t('roles.emptyDesc'), action: canCreate ? _jsxs(Button, { size: "sm", onClick: openCreate, disabled: !catalog.data, children: [_jsx(Plus, {}), t('roles.newRole')] }) : undefined })) : (_jsx("ul", { className: "divide-border divide-y", children: roles.data.map((role) => (_jsxs("li", { className: "flex flex-wrap items-center gap-x-4 gap-y-2 p-4", children: [_jsx("div", { className: "bg-primary/10 text-primary grid size-9 shrink-0 place-items-center rounded-lg", children: _jsx(Shield, { className: "size-4" }) }), _jsxs("div", { className: "min-w-0 flex-1", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("span", { className: "truncate font-medium", children: role.name }), role.isSystem && (_jsxs(Badge, { variant: "secondary", className: "gap-1", children: [_jsx(Lock, { className: "size-3" }), t('roles.builtIn')] }))] }), _jsxs("p", { className: "text-muted-foreground mt-0.5 flex flex-wrap items-center gap-x-3 text-sm", children: [_jsx("span", { children: role.permissions.includes('*') ? t('roles.allPermissions') : t('roles.permissionCount', { count: role.permissions.length }) }), _jsxs("span", { className: "inline-flex items-center gap-1", children: [_jsx(Users, { className: "size-3.5" }), t('roles.userCount', { count: role.userCount })] })] })] }), _jsxs("div", { className: "flex items-center gap-1", children: [canUpdate && (_jsxs(Button, { variant: "ghost", size: "sm", onClick: () => openEdit(role), disabled: role.isSystem, title: role.isSystem ? t('roles.builtInReadonly') : t('roles.editRole'), children: [_jsx(Pencil, {}), t('actions.edit')] })), canDelete && !role.isSystem && (_jsx(Button, { variant: "ghost", size: "icon", onClick: () => setDeleting(role), "aria-label": t('roles.deleteAria', { name: role.name }), children: _jsx(Trash2, { className: "text-destructive" }) }))] })] }, role.id))) })) }) }), catalog.data && (_jsx(RoleEditorDialog, { open: editorOpen, onOpenChange: setEditorOpen, role: editing, catalog: catalog.data })), _jsx(ConfirmDialog, { open: !!deleting, onOpenChange: (open) => !open && setDeleting(null), title: t('roles.deleteTitle', { name: deleting?.name }), description: deleting && deleting.userCount > 0
                    ? t('roles.deleteDescWithUsers', { count: deleting.userCount })
                    : t('roles.deleteDescPlain'), confirmLabel: t('roles.deleteConfirm'), destructive: true, pending: remove.isPending, onConfirm: () => deleting && remove.mutate(deleting) })] }));
}
function LoadingRows() {
    return (_jsx("ul", { className: "divide-border divide-y", children: [0, 1, 2].map((i) => (_jsxs("li", { className: "flex items-center gap-4 p-4", children: [_jsx(Skeleton, { className: "size-9 rounded-lg" }), _jsxs("div", { className: "grid flex-1 gap-1.5", children: [_jsx(Skeleton, { className: "h-4 w-32" }), _jsx(Skeleton, { className: "h-3 w-40" })] }), _jsx(Skeleton, { className: "h-8 w-16" })] }, i))) }));
}
