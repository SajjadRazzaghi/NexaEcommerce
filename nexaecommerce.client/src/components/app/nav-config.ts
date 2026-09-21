import {
    Activity,
    Boxes, ArrowLeftRight,
    Factory,
    FolderTree,
    Home,
    KeyRound,
    LayoutDashboard,
    Package,
    Palette,
    Settings,
    Shield,
    ShoppingBag,
    Tags,
    User,
    Users,
    Warehouse,
    type LucideIcon,
} from 'lucide-react';

import { PERM } from '@/lib/api/admin';

import { HEALTH_PERM } from '@/lib/api/health';
import { INVENTORY_PERM } from '@/lib/api/inventory';

export type NavItem = {
    titleKey: string;
    to: string;
    icon: LucideIcon;
    permission?: string;
    end?: boolean;
    requiresMultiTenant?: boolean;
    external?: boolean;
};

export type NavSection = {
    labelKey?: string;
    items: NavItem[];
};

/**
 * Authenticated application navigation.
 *
 * Keep storefront/account routes separate from administration routes.
 * Account routes are intentionally permission-free because every
 * authenticated customer should be able to access their own account.
 */
export const NAV: NavSection[] = [
    {
        items: [
            {
                titleKey: 'nav.home',
                to: '/',
                icon: Home,
                end: true,
            },
            {
                titleKey: 'nav.products',
                to: '/products',
                icon: ShoppingBag,
                end: true,
            },
        ],
    },

    {
        items: [
            {
                titleKey: 'account.profile',
                to: '/profile',
                icon: User,
                end: true,
            },
            {
                titleKey: 'fields.orders',
                to: '/orders',
                icon: ShoppingBag,
            },
        ],
    },

    {
        labelKey: 'nav.catalog',
        items: [
            {
                titleKey: 'nav.dashboard',
                to: '/admin',
                icon: LayoutDashboard,
                end: true,
            },
            {
                titleKey: 'nav.products',
                to: '/admin/products',
                icon: ShoppingBag,
            },
            {
                titleKey: 'nav.orders',
                to: '/admin/orders',
                icon: ShoppingBag,
                permission: PERM.ordersManage,
            },
            {
                titleKey: 'nav.shipping',
                to: '/admin/fulfillment',
                icon: Package,
                permission: PERM.ordersManage,
            },
            {
                titleKey: 'nav.categories',
                to: '/admin/categories',
                icon: FolderTree,
                permission: PERM.categoriesRead,
            },
            {
                titleKey: 'nav.brands',
                to: '/admin/brands',
                icon: Tags,
                permission: PERM.brandsRead,
            },
            {
                titleKey: 'nav.manufacturers',
                to: '/admin/manufacturers',
                icon: Factory,
                permission: PERM.manufacturersRead,
            },
        ],
    },

    {
        labelKey: 'nav.inventory',
        items: [
            {
                titleKey: 'nav.inventoryManagement',
                to: '/admin/inventory',
                icon: Boxes,
                permission: INVENTORY_PERM.read,
            },
            {
                titleKey: 'nav.inventoryTransfers',
                to: '/admin/inventory/transfers',
                icon: ArrowLeftRight,
                permission: INVENTORY_PERM.read,
            },
            {
                titleKey: 'nav.warehouses',
                to: '/admin/warehouses',
                icon: Warehouse,
                permission: INVENTORY_PERM.read,
            },
        ],
    },

    {
        labelKey: 'nav.administration',
        items: [
            {
                titleKey: 'nav.users',
                to: '/admin/users',
                icon: Users,
                permission: PERM.usersRead,
            },
            {
                titleKey: 'nav.roles',
                to: '/admin/roles',
                icon: Shield,
                permission: PERM.rolesRead,
            },
            {
                titleKey: 'nav.permissions',
                to: '/admin/permissions',
                icon: KeyRound,
                permission: PERM.rolesRead,
            },
            {
                titleKey: 'nav.appearance',
                to: '/admin/appearance',
                icon: Palette,
                permission: 'appearance.manage',
            },
            {
                titleKey: 'nav.settings',
                to: '/admin/settings',
                icon: Settings,
                permission: PERM.settingsRead,
            },
            {
                titleKey: 'nav.health',
                to: '/admin/health',
                icon: Activity,
                permission: HEALTH_PERM.read,
            },
        ],
    },
];
