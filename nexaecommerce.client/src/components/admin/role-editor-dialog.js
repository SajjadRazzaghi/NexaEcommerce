import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';
import { toast } from 'sonner';
import { rolesApi } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
// Expand a role's stored permissions (which may be wildcards) into the concrete set the picker shows.
function expand(permissions, all) {
    const set = new Set();
    for (const p of permissions) {
        if (p === '*')
            all.forEach((a) => set.add(a));
        else if (p.endsWith('.*'))
            all.filter((a) => a.startsWith(p.slice(0, -1))).forEach((a) => set.add(a));
        else
            set.add(p);
    }
    return set;
}
/**
 * Create or edit a role. The permission picker is a flat, grouped list of concrete permissions; an
 * existing role's wildcards are expanded for display and the selection is saved as the concrete set
 * (equivalent grants, unambiguous to round-trip). `role === null` means create. The inner form is
 * keyed by role id so opening it for a different role remounts with fresh state — no effect sync.
 */
export function RoleEditorDialog({ open, onOpenChange, role, catalog, }) {
    return (_jsx(Dialog, { open: open, onOpenChange: onOpenChange, children: _jsx(DialogContent, { className: "max-h-[90svh] gap-0 overflow-hidden p-0 sm:max-w-2xl", children: _jsx(RoleEditorForm, { role: role, catalog: catalog, onClose: () => onOpenChange(false) }, role?.id ?? 'new') }) }));
}
function RoleEditorForm({ role, catalog, onClose }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const allNames = useMemo(() => catalog.flatMap((g) => g.permissions.map((p) => p.name)), [catalog]);
    const [name, setName] = useState(role?.name ?? '');
    const [nameError, setNameError] = useState(null);
    const [selected, setSelected] = useState(() => expand(role?.permissions ?? [], allNames));
    const save = useMutation({
        mutationFn: (body) => role ? rolesApi.update(role.id, body) : rolesApi.create(body),
        onSuccess: () => {
            toast.success(role ? t('roles.updated') : t('roles.created'));
            queryClient.invalidateQueries({ queryKey: ['admin', 'roles'] });
            onClose();
        },
        onError: (error) => {
            if (isApiError(error) && error.fieldErrors?.name) {
                setNameError(error.fieldErrors.name[0]);
                return;
            }
            toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('roles.saveError'));
        },
    });
    const toggle = (permission, on) => setSelected((prev) => {
        const next = new Set(prev);
        if (on)
            next.add(permission);
        else
            next.delete(permission);
        return next;
    });
    const toggleGroup = (group, on) => setSelected((prev) => {
        const next = new Set(prev);
        for (const p of group.permissions) {
            if (on)
                next.add(p.name);
            else
                next.delete(p.name);
        }
        return next;
    });
    const submit = () => {
        if (!name.trim()) {
            setNameError(t('roles.nameRequired'));
            return;
        }
        save.mutate({ name: name.trim(), permissions: [...selected] });
    };
    return (_jsxs(_Fragment, { children: [_jsxs(DialogHeader, { className: "border-b p-6", children: [_jsx(DialogTitle, { children: role ? t('roles.editTitle', { name: role.name }) : t('roles.newTitle') }), _jsx(DialogDescription, { children: t('roles.editorDesc') })] }), _jsxs("div", { className: "grid max-h-[55svh] gap-5 overflow-y-auto p-6", children: [_jsxs("div", { className: "grid gap-2", children: [_jsx(Label, { htmlFor: "role-name", children: t('roles.name') }), _jsx(Input, { id: "role-name", value: name, onChange: (e) => {
                                    setName(e.target.value);
                                    setNameError(null);
                                }, "aria-invalid": !!nameError, autoFocus: true }), nameError && _jsx("p", { className: "text-destructive text-sm", children: nameError })] }), _jsxs("fieldset", { className: "grid gap-4", children: [_jsx("legend", { className: "text-sm font-medium", children: t('nav.permissions') }), catalog.map((group) => {
                                const groupNames = group.permissions.map((p) => p.name);
                                const allOn = groupNames.every((n) => selected.has(n));
                                const someOn = !allOn && groupNames.some((n) => selected.has(n));
                                return (_jsxs("div", { className: "rounded-lg border", children: [_jsxs("label", { className: "hover:bg-muted/40 flex cursor-pointer items-center gap-2 border-b px-3 py-2", children: [_jsx(Checkbox, { checked: allOn ? true : someOn ? 'indeterminate' : false, onCheckedChange: (v) => toggleGroup(group, v === true) }), _jsx("span", { className: "font-medium capitalize", children: group.name })] }), _jsx("div", { className: "grid gap-1 p-2 sm:grid-cols-2", children: group.permissions.map((permission) => (_jsxs("label", { className: "hover:bg-muted/40 flex cursor-pointer items-start gap-2 rounded-md p-2", children: [_jsx(Checkbox, { checked: selected.has(permission.name), onCheckedChange: (v) => toggle(permission.name, v === true) }), _jsxs("span", { className: "grid gap-0.5", children: [_jsx("span", { className: "text-sm leading-none", children: permission.description }), _jsx("code", { className: "text-muted-foreground text-xs", children: permission.name })] })] }, permission.name))) })] }, group.name));
                            })] })] }), _jsxs(DialogFooter, { className: "border-t p-6", children: [_jsx(Button, { variant: "outline", onClick: onClose, disabled: save.isPending, children: t('common.cancel') }), _jsxs(Button, { onClick: submit, disabled: save.isPending, children: [save.isPending && _jsx(Loader2, { className: "animate-spin" }), role ? t('common.saveChanges') : t('roles.createRole')] })] })] }));
}
