import {
  Activity,
  Home,
  KeyRound,
  Settings,
  Shield,
  Users,
  ShoppingBag,
  Tags,
  FolderTree,
  Factory,
  LayoutDashboard,
  type LucideIcon,
} from 'lucide-react';

import { PERM } from '@/lib/api/admin';
import { HEALTH_PERM } from '@/lib/api/health';

export type NavItem = {
  titleKey: string;
  to: string;
  icon: LucideIcon;
  permission?: string;
  end?: boolean;
  requiresMultiTenant?: boolean;
  external?: boolean;
};
export type NavSection = { labelKey?: string; items: NavItem[] };

/**
 * Admin/app navigation contains only routes that currently exist in the client.
 * Keep this list grouped so newly completed modules can be added without mixing
 * storefront, catalog and system administration concerns.
 */
export const NAV: NavSection[] = [
  {
    items: [
          { titleKey: 'nav.home', to: '/', icon: Home, end: true },
          { titleKey: 'nav.products', to: '/products', icon: ShoppingBag, end: true },
    ],
  },
  {
    labelKey: 'nav.catalog',
    items: [
      { titleKey: 'nav.dashboard', to: '/admin', icon: LayoutDashboard, end: true },
      { titleKey: 'nav.products', to: '/admin/products', icon: ShoppingBag },
      { titleKey: 'nav.categories', to: '/admin/categories', icon: FolderTree, permission: PERM.categoriesRead },
      { titleKey: 'nav.brands', to: '/admin/brands', icon: Tags, permission: PERM.brandsRead },
      { titleKey: 'nav.manufacturers', to: '/admin/manufacturers', icon: Factory, permission: PERM.manufacturersRead },
    ],
  },
  {
    labelKey: 'nav.administration',
    items: [
      { titleKey: 'nav.users', to: '/admin/users', icon: Users, permission: PERM.usersRead },
      { titleKey: 'nav.roles', to: '/admin/roles', icon: Shield, permission: PERM.rolesRead },
      { titleKey: 'nav.permissions', to: '/admin/permissions', icon: KeyRound, permission: PERM.rolesRead },
      { titleKey: 'nav.settings', to: '/admin/settings', icon: Settings, permission: PERM.settingsRead },
      { titleKey: 'nav.health', to: '/admin/health', icon: Activity, permission: HEALTH_PERM.read },
    ],
  },
];
