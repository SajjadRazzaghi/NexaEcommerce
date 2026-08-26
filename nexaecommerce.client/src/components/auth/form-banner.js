import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { AlertCircle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
/**
 * Top-of-form error banner: plain-language message with the traceId tucked into a fold-out (never
 * raw JSON). Renders nothing when there's no banner-level message (field errors show inline).
 */
export function FormBanner({ state }) {
    const { t } = useTranslation();
    if (!state?.message)
        return null;
    return (_jsxs(Alert, { variant: "destructive", children: [_jsx(AlertCircle, {}), _jsxs(AlertDescription, { children: [_jsx("p", { children: state.message }), state.traceId && (_jsxs("details", { className: "text-destructive/70 mt-1 text-xs", children: [_jsx("summary", { className: "cursor-pointer select-none", children: t('common.technicalDetails') }), _jsx("code", { className: "break-all", children: t('auth.trace', { id: state.traceId }) })] }))] })] }));
}
