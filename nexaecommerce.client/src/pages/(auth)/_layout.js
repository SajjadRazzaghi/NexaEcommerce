import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { Navigate, Outlet, useSearchParams } from 'react-router';
import { Globe, ShieldCheck, Zap } from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';
import { FullScreenLoader } from '@/components/full-screen-loader';
import { Brand } from '@/components/brand';
import { ModeToggle } from '@/components/mode-toggle';
import { LanguageToggle } from '@/components/language-toggle';
// Shared shell for the unauthenticated screens. Already-signed-in visitors are bounced to their
// destination (returnUrl) or the app root. Split layout: brand showcase left, form right; on
// mobile the showcase drops and the form centers.
export default function AuthLayout() {
    const { isAuthenticated, isLoading } = useAuth();
    const [params] = useSearchParams();
    if (isLoading)
        return _jsx(FullScreenLoader, {});
    if (isAuthenticated) {
        const returnUrl = params.get('returnUrl');
        return _jsx(Navigate, { to: returnUrl && returnUrl.startsWith('/') ? returnUrl : '/', replace: true });
    }
    return (_jsxs("div", { className: "grid min-h-svh lg:grid-cols-2", children: [_jsx(BrandPanel, {}), _jsxs("div", { className: "relative flex flex-col items-center justify-center px-4 py-12 sm:px-8", children: [_jsxs("div", { className: "absolute end-4 top-4 flex items-center gap-1", children: [_jsx(LanguageToggle, {}), _jsx(ModeToggle, {})] }), _jsx("div", { className: "mb-8 lg:hidden", children: _jsx(Brand, { className: "text-lg" }) }), _jsx("main", { className: "w-full max-w-sm", children: _jsx(Outlet, {}) })] })] }));
}
// Always-dark showcase: a branded surface that stays rich in both themes (it must not follow the
// theme's primary token, which inverts to near-white in dark mode).
function BrandPanel() {
    const { t } = useTranslation();
    return (_jsxs("div", { className: "relative hidden flex-col justify-between overflow-hidden bg-gradient-to-b from-slate-900 to-slate-950 p-12 text-slate-50 lg:flex", children: [_jsx("div", { "aria-hidden": true, className: "pointer-events-none absolute -top-1/4 -right-1/4 size-[40rem] rounded-full bg-white/5 blur-3xl" }), _jsx(Brand, { tone: "onDark", className: "relative text-lg" }), _jsxs("div", { className: "relative max-w-md space-y-6", children: [_jsx("h1", { className: "text-3xl leading-tight font-semibold tracking-tight text-balance", children: t('auth.brand.tagline') }), _jsx("p", { className: "text-balance text-slate-300", children: t('auth.brand.subtitle') }), _jsxs("ul", { className: "space-y-3 text-sm text-slate-200", children: [_jsx(Feature, { icon: ShieldCheck, children: t('auth.brand.feature1') }), _jsx(Feature, { icon: Zap, children: t('auth.brand.feature2') }), _jsx(Feature, { icon: Globe, children: t('auth.brand.feature3') })] })] }), _jsxs("p", { className: "relative text-xs text-slate-500", children: ["\u00A9 ", new Date().getFullYear(), " NexaECommerce"] })] }));
}
function Feature({ icon: Icon, children }) {
    return (_jsxs("li", { className: "flex items-center gap-3", children: [_jsx("span", { className: "grid size-8 place-items-center rounded-lg bg-white/10", children: _jsx(Icon, { className: "size-4" }) }), children] }));
}
