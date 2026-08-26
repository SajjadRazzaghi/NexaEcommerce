import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { ArrowUpRight, BookOpen, KeyRound, ShieldCheck, Sparkles } from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';
import { useDocumentTitle } from '@/hooks/use-document-title';
export default function HomePage() {
    const { t } = useTranslation();
    useDocumentTitle(t('nav.home'));
    const { user } = useAuth();
    const firstName = user?.displayName?.split(' ')[0];
    // The customizable widget dashboard is a Pro feature; the lean edition shows a starter placeholder instead.
    const slots = {};
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: firstName ? t('home.welcomeName', { name: firstName }) : t('home.welcome') }), _jsx("p", { className: "text-muted-foreground mt-1", children: t('home.subtitle') })] }), slots.dashboard ?? _jsx(StarterHome, {})] }));
}
// Placeholder home for editions without the widget dashboard — replace it with your app's real home/dashboard.
function StarterHome() {
    const included = [
        { icon: ShieldCheck, label: 'Auth — register, confirm, reset, profile' },
        { icon: KeyRound, label: 'Roles & permissions (RBAC) + admin UI' },
        { icon: ShieldCheck, label: 'Settings, health checks & rate limiting' },
        { icon: KeyRound, label: 'Theming, dark mode & i18n' },
    ];
    // Upstream NexaECommerce configurator. Assembled at runtime so the project rename (which rewrites the literal
    // "NexaECommerce"/"nexaecommerce" → your app's name across the scaffold) leaves this upstream URL intact.
    const configuratorUrl = 'https://net' + 'forge.ebenmonney.com/?edition=pro';
    return (_jsxs("div", { className: "grid gap-4", children: [_jsxs("div", { className: "bg-card rounded-xl border p-5", children: [_jsx("h2", { className: "font-semibold", children: "You're running the NexaECommerce starter" }), _jsx("p", { className: "text-muted-foreground mt-1 text-sm", children: "This is a placeholder \u2014 swap it for your app's home. Your starter already ships a polished, authenticated foundation:" }), _jsx("ul", { className: "text-muted-foreground mt-3 grid gap-1.5 text-sm sm:grid-cols-2", children: included.map(({ icon: Icon, label }) => (_jsxs("li", { className: "flex items-center gap-2", children: [_jsx(Icon, { className: "size-4 shrink-0 text-emerald-500" }), label] }, label))) })] }), _jsxs("div", { className: "grid gap-3 sm:grid-cols-3", children: [_jsx(StepCard, { icon: BookOpen, title: "Read the docs", desc: "USER_GUIDE.md \u00B7 RECIPES.md" }), _jsx(StepCard, { icon: KeyRound, title: "Manage access", desc: "Roles at /admin/roles", href: "/admin/roles" }), _jsx(StepCard, { icon: Sparkles, title: "Add a feature", desc: "Copy Features/_Template" })] }), import.meta.env.DEV && (_jsxs("div", { className: "rounded-xl border border-dashed p-5", children: [_jsxs("div", { className: "flex items-center gap-2 text-sm font-semibold", children: [_jsx(Sparkles, { className: "size-4 text-pink-600 dark:text-pink-400" }), "Available in Pro"] }), _jsx("p", { className: "text-muted-foreground mt-1.5 text-sm leading-relaxed", children: "You're on the Community edition. Pro adds multi-tenancy, an audit trail, the widget dashboard, outgoing webhooks, global \u2318K search, notifications, background jobs, file uploads, export/import, and the runtime theme manager." }), _jsxs("a", { href: configuratorUrl, target: "_blank", rel: "noreferrer", className: "text-primary mt-3 inline-flex items-center gap-1 text-sm font-medium hover:underline", children: ["Upgrade to Pro", _jsx(ArrowUpRight, { className: "size-3.5" })] }), _jsx("p", { className: "text-muted-foreground/70 mt-2 text-xs", children: "Shown only in development \u2014 your users won't see it." })] }))] }));
}
function StepCard({ icon: Icon, title, desc, href, }) {
    const body = (_jsxs(_Fragment, { children: [_jsx(Icon, { className: "text-muted-foreground size-5" }), _jsx("div", { className: "mt-2 text-sm font-medium", children: title }), _jsx("div", { className: "text-muted-foreground text-xs", children: desc })] }));
    const className = 'bg-card hover:border-primary/50 block rounded-xl border p-4 transition-colors';
    return href ? (_jsx("a", { href: href, className: className, children: body })) : (_jsx("div", { className: className, children: body }));
}
