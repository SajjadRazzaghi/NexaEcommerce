import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { ModeToggle } from '@/components/mode-toggle';
import { LanguageToggle } from '@/components/language-toggle';
import { UserMenu } from '@/components/app/user-menu';
import { Separator } from '@/components/ui/separator';
import { SidebarTrigger } from '@/components/ui/sidebar';
// App top bar (inside <SidebarInset>): the sidebar collapse toggle, an optional tenant switcher, a
// prominent centred command palette (the multi-purpose search), and the theme/notifications/account
// controls. The mobile nav drawer is the sidebar's own off-canvas sheet, opened by the same trigger.
export function AppTopbar() {
    // Optional topbar pieces — built in TS so the JSX needs no build-time conditional.
    let whatsNew = null;
    let commandPalette = null;
    let notificationBell = null;
    let tenantSwitcher = null;
    return (_jsxs("header", { className: "bg-background/80 sticky top-0 z-30 flex h-14 items-center gap-2 border-b px-3 backdrop-blur sm:px-4", children: [_jsx(SidebarTrigger, { className: "text-muted-foreground" }), _jsx(Separator, { orientation: "vertical", className: "me-1 hidden h-5 sm:block" }), tenantSwitcher, _jsx("div", { className: "flex min-w-0 flex-1 justify-center px-1 sm:px-2", children: _jsx("span", { "data-tour": "command", className: "w-full max-w-md", children: commandPalette }) }), _jsxs("div", { className: "flex items-center gap-0.5 sm:gap-1", children: [whatsNew, notificationBell, _jsx(LanguageToggle, {}), _jsx("span", { "data-tour": "theme", className: "flex items-center", children: _jsx(ModeToggle, {}) }), _jsx("span", { "data-tour": "account", className: "flex items-center", children: _jsx(UserMenu, {}) })] })] }));
}
