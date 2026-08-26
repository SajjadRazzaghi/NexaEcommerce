import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { RotateCcw, TriangleAlert } from 'lucide-react';
import { isApiError } from '@/lib/problem';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
/**
 * Designed error state (§7.0): plain-language message, a Retry, and the traceId tucked into a
 * fold-out — never raw JSON. Pass the caught error to pull its traceId automatically.
 */
export function ErrorState({ error, onRetry, retrying = false, message = "We couldn't load this. Please try again.", }) {
    const { t } = useTranslation();
    const traceId = isApiError(error) ? error.traceId : undefined;
    return (_jsxs("div", { className: "flex flex-col items-center gap-3 px-6 py-12 text-center", children: [_jsx("div", { className: "bg-destructive/10 text-destructive grid size-11 place-items-center rounded-full", children: _jsx(TriangleAlert, { className: "size-5" }) }), _jsx("p", { className: "text-muted-foreground max-w-sm text-sm", children: message }), _jsxs(Button, { variant: "outline", size: "sm", onClick: onRetry, disabled: retrying, children: [_jsx(RotateCcw, {}), t('common.retry')] }), traceId && (_jsxs("details", { className: "text-muted-foreground/70 mt-1 text-xs", children: [_jsx("summary", { className: "cursor-pointer select-none", children: t('common.technicalDetails') }), _jsxs("code", { className: "break-all", children: ["trace: ", traceId] })] }))] }));
}
/**
 * Designed empty state (§7.0): icon + headline + helper sentence + optional primary action. Never a
 * bare "No results found."
 */
export function EmptyState({ icon: Icon, title, description, action, }) {
    return (_jsxs("div", { className: "flex flex-col items-center gap-3 px-6 py-12 text-center", children: [_jsx("div", { className: "bg-muted text-muted-foreground grid size-12 place-items-center rounded-full", children: _jsx(Icon, { className: "size-6" }) }), _jsxs("div", { className: "grid gap-1", children: [_jsx("p", { className: "font-medium", children: title }), _jsx("p", { className: "text-muted-foreground mx-auto max-w-sm text-sm", children: description })] }), action] }));
}
/**
 * Designed loading state (§7.0): skeletons shaped like the content they replace — never a bare
 * spinner. `rows`/`cols` size a list or table placeholder.
 */
export function LoadingSkeleton({ variant = 'list', rows = 5, cols = 4, className, }) {
    if (variant === 'table') {
        return (_jsx("div", { className: cn('space-y-2.5', className), "aria-busy": true, children: Array.from({ length: rows }).map((_, r) => (_jsx("div", { className: "flex items-center gap-4", children: Array.from({ length: cols }).map((_, c) => (_jsx(Skeleton, { className: cn('h-4 flex-1', c === 0 && 'max-w-[1.25rem]') }, c))) }, r))) }));
    }
    if (variant === 'cards') {
        return (_jsx("div", { className: cn('grid gap-3 sm:grid-cols-2 lg:grid-cols-3', className), "aria-busy": true, children: Array.from({ length: rows }).map((_, i) => (_jsx(Skeleton, { className: "h-28 w-full rounded-xl" }, i))) }));
    }
    return (_jsx("div", { className: cn('space-y-3', className), "aria-busy": true, children: Array.from({ length: rows }).map((_, i) => (_jsxs("div", { className: "flex items-center gap-3", children: [_jsx(Skeleton, { className: "size-9 shrink-0 rounded-full" }), _jsxs("div", { className: "flex-1 space-y-2", children: [_jsx(Skeleton, { className: "h-3.5 w-1/3" }), _jsx(Skeleton, { className: "h-3 w-2/3" })] })] }, i))) }));
}
/** Page header: title + optional description and trailing actions. Consistent spacing app-wide. */
export function PageHeader({ title, description, actions, className, }) {
    return (_jsxs("header", { className: cn('flex flex-wrap items-end justify-between gap-3', className), children: [_jsxs("div", { className: "min-w-0", children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: title }), description && _jsx("p", { className: "text-muted-foreground mt-1", children: description })] }), actions && _jsx("div", { className: "flex items-center gap-2", children: actions })] }));
}
