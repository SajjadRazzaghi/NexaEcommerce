import { Outlet } from 'react-router';

import { useTranslation } from 'react-i18next';

import {
    ShieldAlert,
} from 'lucide-react';

import {
    useAuth,
} from '@/hooks/use-auth';

import {
    hasAnyPermission,
} from '@/lib/permissions';

import {
    PERM,
} from '@/lib/api/admin';

import {
    HEALTH_PERM,
} from '@/lib/api/health';

import {
    INVENTORY_PERM,
} from '@/lib/api/inventory';

import {
    EmptyState,
} from '@/components/data-states';

export default function AdminLayout() {
    const { t } =
        useTranslation();

    const { user } =
        useAuth();

    const canEnter =
        hasAnyPermission(
            user?.permissions ?? [],
            [
                PERM.usersRead,
                PERM.brandsRead,
                PERM.manufacturersRead,
                PERM.categoriesRead,
                PERM.productsRead,
                PERM.ordersRead,
                PERM.ordersManage,
                PERM.rolesRead,
                PERM.settingsRead,
                PERM.auditRead,
                PERM.webhooksRead,
                INVENTORY_PERM.read,
                HEALTH_PERM.read,
            ],
        );

    if (!canEnter) {
        return (
            <EmptyState
                icon={ShieldAlert}
                title={t(
                    'admin.noAccessTitle',
                )}
                description={t(
                    'admin.noAccessDesc',
                )}
            />
        );
    }

    return <Outlet />;
}
