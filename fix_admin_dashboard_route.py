from pathlib import Path
import sys

OLD_MARKER = "// /admin has no page of its own — send the user to the first section they can open."
FIX_MARKER = "/* ADMIN_DASHBOARD_HOME: renders a real landing page instead of redirecting to the first permissioned section. */"

NEW_CONTENT = '''import { ArrowUpRight } from 'lucide-react';
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
'''


def main() -> int:
    if len(sys.argv) > 1:
        project_root = Path(sys.argv[1]).expanduser().resolve()
    else:
        project_root = Path.cwd()

    target = project_root / 'nexaecommerce.client' / 'src' / 'pages' / '(app)' / 'admin' / 'index.tsx'
    if not target.is_file():
        print(f'ERROR: Admin index file not found: {target}', file=sys.stderr)
        print('Run this script from the repository root or pass the repository path as its first argument.', file=sys.stderr)
        return 2

    original = target.read_text(encoding='utf-8-sig')
    if FIX_MARKER in original:
        print('Admin dashboard route is already fixed; no changes made.')
        return 0
    if OLD_MARKER not in original:
        print('ERROR: Admin index content does not match the expected main-branch version; refusing to overwrite it.', file=sys.stderr)
        return 3

    backup = target.with_name(target.name + '.bak_admin_dashboard_route')
    if not backup.exists():
        backup.write_text(original, encoding='utf-8', newline='')

    target.write_text(NEW_CONTENT, encoding='utf-8', newline='')
    print(f'Updated: {target}')
    print(f'Backup:  {backup}')
    print('The /admin route now renders a permission-filtered dashboard instead of redirecting to /admin/brands.')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
