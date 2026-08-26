import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { CircleCheck, Loader2, TriangleAlert } from 'lucide-react';
import { authApi } from '@/lib/api/auth';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { Button } from '@/components/ui/button';
import { meta } from './meta';
export default function VerifyEmailPage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    const [params] = useSearchParams();
    const userId = params.get('userId');
    const token = params.get('token');
    // Confirmation is modelled as a query, not a mutation-fired-from-an-effect. That earlier pattern
    // got permanently stuck on the spinner: under StrictMode's mount→unmount→remount, the run-once ref
    // guard suppressed the re-fire while the mutation observer reset to idle, so a *successful* (200)
    // confirmation never surfaced as success. A query keeps its outcome in the query cache keyed by the
    // link, so it survives the remount and any re-render — it resolves once and the result sticks. The
    // confirm endpoint is idempotent server-side, so even a retry is harmless.
    const confirm = useQuery({
        queryKey: ['confirm-email', userId, token],
        queryFn: () => authApi.confirmEmail({ userId: userId, token: token }),
        enabled: !!userId && !!token,
        retry: false,
        staleTime: Infinity,
        gcTime: Infinity,
    });
    if (!userId || !token) {
        return (_jsx(State, { tone: "error", icon: _jsx(TriangleAlert, { className: "size-6" }), title: t('auth.verify.invalidTitle'), body: t('auth.verify.invalidDesc'), action: _jsx(Link, { to: "/register", children: t('auth.verify.backToSignUp') }) }));
    }
    if (confirm.isSuccess) {
        return (_jsx(State, { tone: "success", icon: _jsx(CircleCheck, { className: "size-6" }), title: t('auth.verify.confirmedTitle'), body: t('auth.verify.confirmedDesc'), action: _jsx(Link, { to: "/login", children: t('auth.verify.goSignIn') }), primary: true }));
    }
    if (confirm.isError) {
        return (_jsx(State, { tone: "error", icon: _jsx(TriangleAlert, { className: "size-6" }), title: t('auth.verify.failedTitle'), body: t('auth.verify.failedDesc'), action: _jsx(Link, { to: "/login", children: t('auth.backToSignIn') }) }));
    }
    return (_jsxs("div", { className: "grid gap-4 text-center", role: "status", "aria-live": "polite", children: [_jsx(Loader2, { className: "text-muted-foreground mx-auto size-8 animate-spin" }), _jsx("h1", { className: "text-xl font-semibold tracking-tight", children: t('auth.verify.confirmingTitle') }), _jsx("p", { className: "text-muted-foreground text-sm", children: t('auth.verify.confirmingDesc') })] }));
}
function State({ tone, icon, title, body, action, primary = false, }) {
    const tones = {
        success: 'bg-success/10 text-success',
        error: 'bg-destructive/10 text-destructive',
    };
    return (_jsxs("div", { className: "grid gap-4 text-center", children: [_jsx("div", { className: `mx-auto grid size-12 place-items-center rounded-full ${tones[tone]}`, children: icon }), _jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: title }), _jsx("p", { className: "text-muted-foreground text-sm text-balance", children: body }), _jsx(Button, { asChild: true, variant: primary ? 'default' : 'outline', className: "mt-2", children: action })] }));
}
