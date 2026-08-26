import { useEffect, type ReactNode } from 'react';
import { Outlet } from 'react-router';
import { Provider } from 'react-redux';
import { QueryClientProvider } from '@tanstack/react-query';

import { store } from '@/store';
import { useAuth } from '@/hooks/use-auth';
import { ThemeProvider } from '@/components/theme-provider';
import { BrandColor } from '@/components/app/brand-color';
import { Toaster } from '@/components/ui/sonner';
import { queryClient } from '@/lib/query-client';
import i18n, { directionOf } from '@/i18n.config';

// Keep <html lang> and <html dir> in sync with the active language
// so the page mirrors correctly for RTL scripts.
function useDocumentLanguage() {
    useEffect(() => {
        const apply = (lng: string) => {
            const root = document.documentElement;

            root.lang = lng;
            root.dir = directionOf(lng);
        };

        apply(i18n.language);

        i18n.on('languageChanged', apply);

        return () => {
            i18n.off('languageChanged', apply);
        };
    }, []);
}

// Once signed in, adopt the user's saved language.
function AuthLocaleSync() {
    const { user } = useAuth();

    useEffect(() => {
        if (
            user?.locale &&
            i18n.resolvedLanguage !== user.locale
        ) {
            i18n.changeLanguage(user.locale);
        }
    }, [user?.locale]);

    return null;
}

export default function App() {
    useDocumentLanguage();

    const slots: Record<string, ReactNode> = {};

    return (
        <Provider store={store}>
            <QueryClientProvider client={queryClient}>
                <ThemeProvider
                    defaultTheme="system"
                    storageKey="nexaecommerce-theme"
                >
                    <BrandColor />

                    <AuthLocaleSync />

                    <Outlet />

                    <Toaster />

                    {slots.pwaPrompts}
                </ThemeProvider>
            </QueryClientProvider>
        </Provider>
    );
}