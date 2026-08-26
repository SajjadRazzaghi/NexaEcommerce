import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity, CheckCircle2, Database, HardDrive, RefreshCw, Server, ShieldAlert, TriangleAlert, XCircle, } from 'lucide-react';
import { healthApi, HEALTH_PERM } from '@/lib/api/health';
import { usePermission } from '@/hooks/use-permission';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { BadgeCell } from '@/components/data-grid';
import { EmptyState, ErrorState, LoadingSkeleton, PageHeader } from '@/components/data-states';
import { useState } from 'react';
import { meta } from './meta';
const REFRESH_MS = 15000;
export default function HealthPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const canRead = usePermission(HEALTH_PERM.read);
    const [autoRefresh, setAutoRefresh] = useState(true);
    const query = useQuery({
        queryKey: ['admin', 'health'],
        queryFn: healthApi.get,
        enabled: canRead,
        refetchInterval: autoRefresh ? REFRESH_MS : false,
        refetchOnWindowFocus: true,
    });
    if (!canRead) {
        return (_jsx(EmptyState, { icon: ShieldAlert, title: t('health.noAccessTitle'), description: t('health.noAccessDesc') }));
    }
    const report = query.data;
    const controls = (_jsxs("div", { className: "flex items-center gap-4", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx(Switch, { id: "auto-refresh", checked: autoRefresh, onCheckedChange: setAutoRefresh }), _jsx(Label, { htmlFor: "auto-refresh", className: "text-muted-foreground text-sm font-normal", children: t('health.autoRefresh') })] }), _jsxs(Button, { variant: "outline", size: "sm", onClick: () => query.refetch(), disabled: query.isFetching, children: [_jsx(RefreshCw, { className: cn(query.isFetching && 'animate-spin') }), t('health.refresh')] })] }));
    return (_jsxs("div", { className: "grid gap-6", children: [_jsx(PageHeader, { title: t('nav.health'), description: t('pages.healthDesc'), actions: controls }), query.isLoading ? (_jsxs("div", { className: "grid gap-6", children: [_jsx(LoadingSkeleton, { variant: "cards", rows: 1, className: "sm:grid-cols-1" }), _jsx(LoadingSkeleton, { variant: "cards", rows: 3 })] })) : query.isError || !report ? (_jsx("div", { className: "rounded-xl border", children: _jsx(ErrorState, { error: query.error, onRetry: () => query.refetch(), retrying: query.isFetching, message: t('health.loadError') }) })) : (_jsxs(_Fragment, { children: [_jsx(OverallBanner, { status: report.status, checkedAt: report.checkedAt, durationMs: report.totalDurationMs }), report.checks.length === 0 ? (_jsx(EmptyState, { icon: Activity, title: t('health.noChecksTitle'), description: t('health.noChecksDesc') })) : (_jsx("div", { className: "grid gap-4 sm:grid-cols-2 lg:grid-cols-3", children: report.checks.map((check) => (_jsx(CheckCard, { ...check }, check.name))) }))] }))] }));
}
function OverallBanner({ status, checkedAt, durationMs }) {
    const { t } = useTranslation();
    const s = STATUS[status];
    const key = status.toLowerCase();
    return (_jsx(Card, { className: cn('border-l-4', s.borderClass), children: _jsxs(CardContent, { className: "flex flex-wrap items-center gap-4 py-5", children: [_jsx("div", { className: cn('grid size-12 shrink-0 place-items-center rounded-xl', s.iconWrapClass), children: _jsx(s.icon, { className: "size-6" }) }), _jsxs("div", { className: "min-w-0 flex-1", children: [_jsx("p", { className: "text-lg font-semibold", children: t(`health.headline.${key}`) }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('health.lastChecked', { time: new Date(checkedAt).toLocaleTimeString(), ms: Math.round(durationMs) }) })] }), _jsx(BadgeCell, { label: t(`health.status.${key}`), tone: s.tone })] }) }));
}
function CheckCard({ name, status, description, durationMs, tags, error, data }) {
    const { t } = useTranslation();
    const s = STATUS[status];
    const Icon = CHECK_ICONS[name] ?? Activity;
    const entries = Object.entries(data);
    return (_jsx(Card, { className: cn('border-t-2', s.borderClass), children: _jsxs(CardContent, { className: "grid gap-3 py-5", children: [_jsxs("div", { className: "flex items-start justify-between gap-3", children: [_jsxs("div", { className: "flex min-w-0 items-center gap-2", children: [_jsx("span", { className: "text-muted-foreground", children: _jsx(Icon, { className: "size-4" }) }), _jsx("span", { className: "truncate font-medium", children: t(`health.checks.${name}`, { defaultValue: humanize(name) }) })] }), _jsx(BadgeCell, { label: t(`health.status.${status.toLowerCase()}`), tone: s.tone })] }), description && _jsx("p", { className: "text-muted-foreground text-sm", children: description }), entries.length > 0 && (_jsx("dl", { className: "grid gap-1 text-sm", children: entries.map(([key, value]) => (_jsxs("div", { className: "flex items-baseline justify-between gap-3", children: [_jsx("dt", { className: "text-muted-foreground text-xs", children: t(`health.data.${key}`, { defaultValue: humanize(key) }) }), _jsx("dd", { className: "truncate text-end font-mono text-xs", children: value })] }, key))) })), error && (_jsxs("details", { className: "text-sm", children: [_jsx("summary", { className: "text-destructive cursor-pointer select-none text-xs font-medium", children: t('health.errorDetail') }), _jsx("p", { className: "text-muted-foreground mt-1 break-words text-xs", children: error })] })), _jsxs("div", { className: "text-muted-foreground flex items-center justify-between gap-2 text-xs", children: [_jsx("span", { children: t('health.durationMs', { ms: Math.round(durationMs) }) }), tags.length > 0 && _jsx("span", { className: "font-mono", children: tags.join(' · ') })] })] }) }));
}
const STATUS = {
    Healthy: {
        icon: CheckCircle2,
        tone: 'success',
        borderClass: 'border-l-success border-t-success',
        iconWrapClass: 'bg-success/10 text-success',
    },
    Degraded: {
        icon: TriangleAlert,
        tone: 'warning',
        borderClass: 'border-l-warning border-t-warning',
        iconWrapClass: 'bg-warning/10 text-warning',
    },
    Unhealthy: {
        icon: XCircle,
        tone: 'destructive',
        borderClass: 'border-l-destructive border-t-destructive',
        iconWrapClass: 'bg-destructive/10 text-destructive',
    },
};
const CHECK_ICONS = {
    database: Database,
    'background-jobs': Server,
    storage: HardDrive,
};
/** "background-jobs" / "pendingMigrations" → "Background jobs" / "Pending migrations". */
function humanize(value) {
    const spaced = value
        .replace(/[-_]/g, ' ')
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .trim();
    return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}
