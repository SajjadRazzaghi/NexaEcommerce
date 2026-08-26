import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Link, useLocation, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Home } from 'lucide-react';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { Brand } from '@/components/brand';
import { ModeToggle } from '@/components/mode-toggle';
// Custom not-found page (generouted maps 404.tsx to the catch-all route). It renders standalone —
// outside the app shell — so it carries its own brand mark + theme toggle and centers a designed state
// (§7.0), rather than the bare unstyled fallback it replaced.
export default function NotFound() {
    const { t } = useTranslation();
    const navigate = useNavigate();
    const { pathname } = useLocation();
    useDocumentTitle(t('notFound.title'));
    return (_jsxs("div", { className: "bg-background text-foreground relative grid min-h-svh place-items-center px-4", children: [_jsx("div", { className: "absolute start-4 top-4", children: _jsx(Brand, {}) }), _jsx("div", { className: "absolute end-4 top-4", children: _jsx(ModeToggle, {}) }), _jsxs("main", { className: "flex w-full max-w-md flex-col items-center text-center", children: [_jsx("p", { "aria-hidden": true, className: "from-foreground to-muted-foreground bg-gradient-to-b bg-clip-text text-[5.5rem] leading-none font-extrabold tracking-tight text-transparent select-none sm:text-[7rem]", children: "404" }), _jsx("h1", { className: "mt-2 text-2xl font-semibold tracking-tight", children: t('notFound.title') }), _jsx("p", { className: "text-muted-foreground mt-2 text-balance", children: t('notFound.description') }), pathname && pathname !== '/' && (_jsx("code", { className: "bg-muted text-muted-foreground mt-4 max-w-full truncate rounded-md px-2 py-1 font-mono text-xs", children: pathname })), _jsxs("div", { className: "mt-6 flex flex-wrap items-center justify-center gap-3", children: [_jsxs(Button, { variant: "outline", onClick: () => navigate(-1), children: [_jsx(ArrowLeft, {}), t('notFound.goBack')] }), _jsx(Button, { asChild: true, children: _jsxs(Link, { to: "/", children: [_jsx(Home, {}), t('notFound.goHome')] }) })] })] })] }));
}
