import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useState } from 'react';
import { NavLink, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';
import { ChevronRight } from 'lucide-react';
import { useAuth } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { NAV } from '@/components/app/nav-config';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { SidebarGroup, SidebarGroupContent, SidebarGroupLabel, SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar, } from '@/components/ui/sidebar';
/**
 * Primary navigation rendered inside the shadcn <Sidebar>. Each NAV section becomes a SidebarGroup;
 * a *labelled* section is collapsible (its open state persists per user in localStorage), while the
 * unlabelled top section (Home) is always shown. Icons double as tooltips when the rail is collapsed
 * to icons. Selecting an item closes the mobile drawer.
 */
export function SidebarNav() {
    const { user } = useAuth();
    const granted = user?.permissions ?? [];
    // Single-tenant editions hide nothing on this axis; multi-tenant editions hide tenant-only items
    // until the user actually belongs to a switchable tenant.
    let multiTenant = false;
    const sections = NAV.map((section) => ({
        ...section,
        items: section.items.filter((item) => (!item.permission || hasPermission(granted, item.permission)) &&
            (!item.requiresMultiTenant || multiTenant)),
    })).filter((section) => section.items.length > 0);
    return (_jsx(_Fragment, { children: sections.map((section, i) => (_jsx(NavGroup, { section: section }, section.labelKey ?? `s${i}`))) }));
}
function NavGroup({ section }) {
    const menu = (_jsx(SidebarMenu, { children: section.items.map((item) => (_jsx(NavMenuItem, { item: item }, item.to))) }));
    // Unlabelled section (Home): plain group, nothing to collapse.
    if (!section.labelKey) {
        return (_jsx(SidebarGroup, { children: _jsx(SidebarGroupContent, { children: menu }) }));
    }
    return _jsx(CollapsibleNavGroup, { labelKey: section.labelKey, children: menu });
}
function CollapsibleNavGroup({ labelKey, children }) {
    const { t } = useTranslation();
    const [open, setOpen] = usePersistentOpen(`nexaecommerce:nav-group:${labelKey}`, true);
    return (_jsx(Collapsible, { open: open, onOpenChange: setOpen, className: "group/collapsible", children: _jsxs(SidebarGroup, { children: [_jsx(SidebarGroupLabel, { asChild: true, className: "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground cursor-pointer", children: _jsxs(CollapsibleTrigger, { children: [t(labelKey), _jsx(ChevronRight, { className: "ms-auto size-3.5 transition-transform group-data-[state=open]/collapsible:rotate-90" })] }) }), _jsx(CollapsibleContent, { children: _jsx(SidebarGroupContent, { children: children }) })] }) }));
}
function NavMenuItem({ item }) {
    const { t } = useTranslation();
    const { pathname } = useLocation();
    const { isMobile, setOpenMobile } = useSidebar();
    // External items (e.g. the Hangfire dashboard) are server-rendered pages, not SPA routes — open them
    // in a new tab so the app stays put; same origin, so the auth cookie rides along.
    if (item.external) {
        return (_jsx(SidebarMenuItem, { children: _jsx(SidebarMenuButton, { asChild: true, tooltip: t(item.titleKey), children: _jsxs("a", { href: item.to, target: "_blank", rel: "noopener noreferrer", onClick: () => isMobile && setOpenMobile(false), children: [_jsx(item.icon, {}), _jsx("span", { children: t(item.titleKey) })] }) }) }));
    }
    const active = item.end
        ? pathname === item.to
        : pathname === item.to || pathname.startsWith(`${item.to}/`);
    return (_jsx(SidebarMenuItem, { children: _jsx(SidebarMenuButton, { asChild: true, isActive: active, tooltip: t(item.titleKey), children: _jsxs(NavLink, { to: item.to, end: item.end, onClick: () => isMobile && setOpenMobile(false), children: [_jsx(item.icon, {}), _jsx("span", { children: t(item.titleKey) })] }) }) }));
}
/** Open/closed state backed by localStorage, so a collapsed group stays collapsed across reloads. */
function usePersistentOpen(key, fallback) {
    const [open, setOpen] = useState(() => {
        const stored = localStorage.getItem(key);
        return stored === null ? fallback : stored === 'true';
    });
    const set = (next) => {
        setOpen(next);
        localStorage.setItem(key, String(next));
    };
    return [open, set];
}
