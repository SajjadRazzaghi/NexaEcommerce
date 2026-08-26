import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { Moon, Sun } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, } from '@/components/ui/dropdown-menu';
import { useTheme } from '@/components/theme-provider';
export function ModeToggle() {
    const { setTheme } = useTheme();
    const { t } = useTranslation();
    return (_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsxs(Button, { variant: "ghost", size: "icon", children: [_jsx(Sun, { className: "size-[1.2rem] scale-100 rotate-0 transition-all dark:scale-0 dark:-rotate-90" }), _jsx(Moon, { className: "absolute size-[1.2rem] scale-0 rotate-90 transition-all dark:scale-100 dark:rotate-0" }), _jsx("span", { className: "sr-only", children: t('theme.toggle') })] }) }), _jsxs(DropdownMenuContent, { align: "end", children: [_jsx(DropdownMenuItem, { onClick: () => setTheme('light'), children: t('theme.light') }), _jsx(DropdownMenuItem, { onClick: () => setTheme('dark'), children: t('theme.dark') }), _jsx(DropdownMenuItem, { onClick: () => setTheme('system'), children: t('theme.system') })] })] }));
}
