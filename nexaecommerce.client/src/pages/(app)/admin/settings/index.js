import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, SlidersHorizontal } from 'lucide-react';
import { toast } from 'sonner';
import { settingsApi, PERM } from '@/lib/api/admin';
import { isApiError } from '@/lib/problem';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { EmptyState, ErrorState } from '@/components/data-states';
import { SectionLayout } from '@/components/section-layout';
import { meta } from './meta';
const settingsKey = ['admin', 'settings'];
export default function SettingsPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const queryClient = useQueryClient();
    const canUpdate = usePermission(PERM.settingsUpdate);
    const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
        queryKey: settingsKey,
        queryFn: settingsApi.list,
    });
    const save = useMutation({
        mutationFn: ({ key, value }) => settingsApi.update(key, value),
        onSuccess: () => {
            toast.success(t('settings.saved'));
            queryClient.invalidateQueries({ queryKey: settingsKey });
        },
        onError: (e) => toast.error(isApiError(e) ? (e.problem.detail ?? e.message) : t('settings.saveError')),
    });
    return (_jsxs("div", { className: "grid gap-4", children: [_jsxs("header", { children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('settings.title') }), _jsx("p", { className: "text-muted-foreground mt-1", children: t('settings.subtitle') })] }), isLoading ? (_jsx(LoadingState, {})) : isError ? (_jsx(Card, { children: _jsx(ErrorState, { error: error, onRetry: () => refetch(), retrying: isFetching, message: t('settings.loadError') }) })) : !data || data.length === 0 ? (_jsx(Card, { children: _jsx(EmptyState, { icon: SlidersHorizontal, title: t('settings.emptyTitle'), description: t('settings.emptyDesc') }) })) : (_jsx(SectionLayout, { side: "end", sections: data.map((group) => ({
                    id: group.category,
                    label: t(`settings.categories.${group.category.toLowerCase()}`, { defaultValue: group.category }),
                    content: (_jsxs(Card, { children: [_jsx(CardHeader, { children: _jsx(CardTitle, { className: "capitalize", children: t(`settings.categories.${group.category.toLowerCase()}`, { defaultValue: group.category }) }) }), _jsx(CardContent, { children: _jsx("ul", { className: "divide-border divide-y", children: group.settings.map((setting) => (_jsx("li", { children: _jsx(SettingRow, { setting: setting, canUpdate: canUpdate, saving: save.isPending && save.variables?.key === setting.key, onSave: (value) => save.mutate({ key: setting.key, value }) }, `${setting.key}:${String(setting.value)}`) }, setting.key))) }) })] })),
                })) }))] }));
}
function SettingRow({ setting, canUpdate, saving, onSave, }) {
    const label = humanize(setting.key);
    return (_jsxs("div", { className: "flex flex-col gap-2 py-4 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between", children: [_jsxs("div", { className: "min-w-0", children: [_jsx("p", { className: "font-medium", children: label }), _jsx("code", { className: "text-muted-foreground text-xs", children: setting.key })] }), setting.kind === 'boolean' ? (_jsx(Switch, { checked: setting.value, disabled: !canUpdate || saving, onCheckedChange: (value) => onSave(value), "aria-label": label })) : setting.kind === 'choice' ? (_jsx(ChoiceSetting, { setting: setting, canUpdate: canUpdate, saving: saving, onSave: onSave })) : (_jsx(TextSetting, { setting: setting, canUpdate: canUpdate, saving: saving, onSave: onSave }))] }));
}
function TextSetting({ setting, canUpdate, saving, onSave, }) {
    const { t } = useTranslation();
    const [draft, setDraft] = useState(String(setting.value));
    const dirty = draft !== String(setting.value);
    const commit = () => {
        if (!dirty)
            return;
        onSave(setting.kind === 'number' ? Number(draft) : draft);
    };
    return (_jsxs("div", { className: "flex items-center gap-2 sm:w-72", children: [_jsx(Input, { type: setting.kind === 'number' ? 'number' : 'text', value: draft, onChange: (e) => setDraft(e.target.value), disabled: !canUpdate || saving, className: "flex-1" }), _jsxs(Button, { size: "sm", variant: "outline", onClick: commit, disabled: !canUpdate || !dirty || saving, children: [saving && _jsx(Loader2, { className: "animate-spin" }), t('common.save')] })] }));
}
function ChoiceSetting({ setting, canUpdate, saving, onSave, }) {
    const options = setting.options ?? [];
    const current = String(setting.value);
    const hasCurrent = options.some((o) => o.value === current);
    return (_jsxs("div", { className: "flex items-center gap-2 sm:w-72", children: [_jsxs("select", { value: current, disabled: !canUpdate || saving, onChange: (e) => e.target.value !== current && onSave(e.target.value), className: "border-input bg-background ring-offset-background focus-visible:ring-ring h-9 flex-1 rounded-md border px-3 text-sm capitalize focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50", children: [!hasCurrent && current && _jsx("option", { value: current, children: current }), options.map((o) => (_jsx("option", { value: o.value, children: o.label }, o.value)))] }), saving && _jsx(Loader2, { className: "text-muted-foreground size-4 animate-spin" })] }));
}
// "Account.AllowRegistration" → "Allow registration"
function humanize(key) {
    const last = key.includes('.') ? key.slice(key.lastIndexOf('.') + 1) : key;
    const spaced = last.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
    return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}
function LoadingState() {
    return (_jsxs(Card, { children: [_jsx(CardHeader, { children: _jsx(Skeleton, { className: "h-5 w-28" }) }), _jsx(CardContent, { className: "grid gap-4", children: [0, 1].map((i) => (_jsxs("div", { className: "flex items-center justify-between", children: [_jsxs("div", { className: "grid gap-1.5", children: [_jsx(Skeleton, { className: "h-4 w-40" }), _jsx(Skeleton, { className: "h-3 w-32" })] }), _jsx(Skeleton, { className: "h-6 w-10 rounded-full" })] }, i))) })] }));
}
