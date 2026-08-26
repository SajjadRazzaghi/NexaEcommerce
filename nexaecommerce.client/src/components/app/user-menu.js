import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { LogOut, Shield, User } from 'lucide-react';
import { useAuth, useLogout } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { PERM } from '@/lib/api/admin';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger, } from '@/components/ui/dropdown-menu';
export function UserMenu() {
    const { user } = useAuth();
    const { t } = useTranslation();
    const navigate = useNavigate();
    const logout = useLogout();
    if (!user)
        return null;
    const signOut = () => logout.mutate(undefined, { onSettled: () => navigate('/login', { replace: true }) });
    // Land on the first admin area the user can actually open; hidden entirely if they can open none.
    const adminHref = hasPermission(user.permissions, PERM.usersRead)
        ? '/admin/users'
        : hasPermission(user.permissions, PERM.rolesRead)
            ? '/admin/roles'
            : hasPermission(user.permissions, PERM.settingsRead)
                ? '/admin/settings'
                : null;
    // Optional account-menu item — built in TS into a const slot so the menu JSX needs no build-time conditional.
    const slots = {};
    return (_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsx(Button, { variant: "ghost", size: "icon", className: "rounded-full", "aria-label": t('account.menuLabel'), children: _jsxs(Avatar, { className: "size-8", children: [_jsx(AvatarImage, { src: user.avatarUrl ?? undefined, alt: "" }), _jsx(AvatarFallback, { children: initials(user.displayName ?? user.email) })] }) }) }), _jsxs(DropdownMenuContent, { align: "end", className: "w-56", children: [_jsxs(DropdownMenuLabel, { className: "grid gap-0.5", children: [_jsx("span", { className: "truncate font-medium", children: user.displayName ?? t('account.menuLabel') }), _jsx("span", { className: "text-muted-foreground truncate text-xs font-normal", children: user.email })] }), _jsx(DropdownMenuSeparator, {}), _jsxs(DropdownMenuItem, { onClick: () => navigate('/profile'), children: [_jsx(User, {}), t('account.profile')] }), slots.tourItem, adminHref && (_jsxs(DropdownMenuItem, { onClick: () => navigate(adminHref), children: [_jsx(Shield, {}), t('account.administration')] })), _jsx(DropdownMenuSeparator, {}), _jsxs(DropdownMenuItem, { onClick: signOut, disabled: logout.isPending, children: [_jsx(LogOut, {}), t('account.signOut')] })] })] }));
}
function initials(value) {
    const parts = value.trim().split(/\s+/);
    if (parts.length >= 2)
        return (parts[0][0] + parts[1][0]).toUpperCase();
    return value.slice(0, 2).toUpperCase();
}
