import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { KeyRound } from 'lucide-react';
import { permissionsApi } from '@/lib/api/admin';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { EmptyState, ErrorState } from '@/components/data-states';
import { SectionLayout } from '@/components/section-layout';
import { meta } from './meta';
export default function PermissionsPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const { data, isLoading, isError, error, refetch, isFetching } = useQuery({
        queryKey: ['admin', 'permissions'],
        queryFn: permissionsApi.catalog,
    });
    return (_jsxs("div", { className: "grid gap-4", children: [_jsxs("header", { children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('permissions.title') }), _jsx("p", { className: "text-muted-foreground mt-1", children: t('permissions.subtitle') })] }), isLoading ? (_jsx(LoadingState, {})) : isError ? (_jsx(Card, { children: _jsx(ErrorState, { error: error, onRetry: () => refetch(), retrying: isFetching, message: t('permissions.loadError') }) })) : !data || data.length === 0 ? (_jsx(Card, { children: _jsx(EmptyState, { icon: KeyRound, title: t('permissions.emptyTitle'), description: t('permissions.emptyDesc') }) })) : (_jsx(SectionLayout, { side: "end", sections: data.map((group) => ({
                    id: group.name,
                    label: t(`permissions.groups.${group.name}`, { defaultValue: group.name }),
                    badge: group.permissions.length,
                    content: (_jsxs(Card, { children: [_jsx(CardHeader, { children: _jsx(CardTitle, { className: "capitalize", children: t(`permissions.groups.${group.name}`, { defaultValue: group.name }) }) }), _jsx(CardContent, { children: _jsx("ul", { className: "divide-border divide-y", children: group.permissions.map((permission) => (_jsxs("li", { className: "flex flex-col gap-1 py-3 first:pt-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between", children: [_jsx("span", { className: "text-foreground", children: permission.description }), _jsx("code", { className: "bg-muted text-muted-foreground w-fit rounded px-1.5 py-0.5 text-xs", children: permission.name })] }, permission.name))) }) })] })),
                })) }))] }));
}
function LoadingState() {
    return (_jsx("div", { className: "grid gap-4", children: [0, 1].map((card) => (_jsxs(Card, { children: [_jsx(CardHeader, { children: _jsx(Skeleton, { className: "h-5 w-28" }) }), _jsx(CardContent, { className: "grid gap-3", children: [0, 1, 2].map((row) => (_jsxs("div", { className: "flex items-center justify-between", children: [_jsx(Skeleton, { className: "h-4 w-48" }), _jsx(Skeleton, { className: "h-4 w-24" })] }, row))) })] }, card))) }));
}
