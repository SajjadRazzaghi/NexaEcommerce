import { type ReactNode } from 'react';

import {
    Navigate,
    Outlet,
    useLocation,
} from 'react-router';

import { useTranslation } from 'react-i18next';

import { useAuth } from '@/hooks/use-auth';

import { FullScreenLoader } from '@/components/full-screen-loader';

import { AppSidebar } from '@/components/app/app-sidebar';

import { AppTopbar } from '@/components/app/app-topbar';

import {
    SidebarInset,
    SidebarProvider,
} from '@/components/ui/sidebar';

import StoreHeader from '@/components/storefront/StoreHeader';

export default function AppLayout() {
    const {
        isAuthenticated,
        isLoading,
    } = useAuth();

    const { t } = useTranslation();

    const location = useLocation();

    /*
     * Public storefront routes.
     *
     * The shopping cart is intentionally NOT rendered
     * inside the admin/application shell.
     */
    const isPublicStorefront =
        location.pathname === '/' ||
        location.pathname === '/cart';

    if (isPublicStorefront) {
        return (
            <div className="min-h-screen bg-background">
                <StoreHeader />

                <main className="min-h-[calc(100vh-73px)]">
                    <Outlet />
                </main>
            </div>
        );
    }

    if (isLoading) {
        return <FullScreenLoader />;
    }

    if (!isAuthenticated) {
        return (
            <Navigate
                to={`/login?returnUrl=${encodeURIComponent(
                    location.pathname,
                )}`}
                replace
            />
        );
    }

    let onboardingTour: ReactNode = null;
    let realtime: ReactNode = null;
    let tenantBranding: ReactNode = null;

    return (
        <SidebarProvider>
            <a
                href="#main-content"
                className="bg-background focus:ring-ring sr-only focus:fixed focus:start-4 focus:top-4 focus:z-50 focus:rounded-md focus:border focus:px-3 focus:py-2 focus:shadow-lg focus:ring-[3px]"
            >
                {t('common.skipToContent')}
            </a>

            {realtime}

            {tenantBranding}

            {onboardingTour}

            <AppSidebar />

            <SidebarInset>
                <AppTopbar />

                <div
                    id="main-content"
                    className="mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6 lg:px-8"
                >
                    <Outlet />
                </div>
            </SidebarInset>
        </SidebarProvider>
    );
}