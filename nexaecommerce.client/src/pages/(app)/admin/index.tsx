import { ArrowUpRight } from 'lucide-react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { NAV } from '@/components/app/nav-config';

/* ADMIN_DASHBOARD_HOME: renders a real landing page instead of redirecting to the first permissioned section. */
export default function AdminIndex() {
    const { user } = useAuth();
    const { t, i18n } = useTranslation();
    const granted = user?.permissions ?? [];
    const isFa = i18n.language.toLowerCase().startsWith('fa');

    const text = isFa
        ? {
              description: 'نمای کلی پنل مدیریت و دسترسی سریع به بخش‌های فروشگاه.',
              open: 'ورود به بخش',
              empty: 'هیچ بخشی برای دسترسی این حساب در دسترس نیست.',
              other: 'بخش‌های مدیریت',
          }
        : {
              description: 'Your administration overview and quick access to store management.',
              open: 'Open section',
              empty: 'No management sections are available for this account.',
              other: 'Management',
          };

    // Keep this overview aligned with the actual sidebar and hide links the user cannot access.
    const sections = NAV
        .map((section) => ({
            ...section,
            items: section.items.filter(
                (item) =>
                    item.to.startsWith('/admin/') &&
                    (!item.permission || hasPermission(granted, item.permission)) &&
                    !item.requiresMultiTenant,
            ),
        }))
        .filter((section) => section.items.length > 0);

    return (
        <div className="grid gap-8" dir={isFa ? 'rtl' : 'ltr'}>
            <header className="grid gap-2">
                <h1 className="text-2xl font-semibold">
                    {t('nav.dashboard')}
                </h1>
                <p className="text-sm text-muted-foreground">
                    {text.description}
                </p>
            </header>

            {sections.length === 0 ? (
                <div className="rounded-xl border border-dashed p-8 text-center text-sm text-muted-foreground">
                    {text.empty}
                </div>
            ) : (
                sections.map((section, index) => (
                    <section key={section.labelKey ?? `admin-section-${index}`} className="grid gap-3">
                        <h2 className="text-base font-semibold">
                            {section.labelKey ? t(section.labelKey) : text.other}
                        </h2>
                        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                            {section.items.map((item) => {
                                const Icon = item.icon;

                                return (
                                    <Link
                                        key={item.to}
                                        to={item.to}
                                        className="group flex min-h-32 flex-col justify-between rounded-xl border bg-card p-4 transition-colors hover:bg-accent/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                                    >
                                        <div className="flex items-start justify-between gap-3">
                                            <span className="flex size-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                                                <Icon className="size-5" />
                                            </span>
                                            <ArrowUpRight className="size-4 text-muted-foreground transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5" />
                                        </div>
                                        <div className="mt-5">
                                            <h3 className="font-medium">
                                                {t(item.titleKey)}
                                            </h3>
                                            <p className="mt-1 text-xs text-muted-foreground">
                                                {text.open}
                                            </p>
                                        </div>
                                    </Link>
                                );
                            })}
                        </div>
                    </section>
                ))
            )}
        </div>
    );
}
