import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { Check, Languages } from 'lucide-react';
import { LANGUAGES } from '@/i18n.config';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger, } from '@/components/ui/dropdown-menu';
// Language switcher. Each option reads in its own script (autonym) with its own direction, so it's
// legible whatever the current locale. The choice is persisted by i18next's localStorage detector.
export function LanguageToggle() {
    const { i18n, t } = useTranslation();
    const current = i18n.resolvedLanguage;
    return (_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsx(Button, { variant: "ghost", size: "icon", "aria-label": t('language.label'), children: _jsx(Languages, {}) }) }), _jsxs(DropdownMenuContent, { align: "end", className: "min-w-40", children: [_jsx(DropdownMenuLabel, { children: t('language.label') }), _jsx(DropdownMenuSeparator, {}), LANGUAGES.map((language) => (_jsxs(DropdownMenuItem, { onClick: () => i18n.changeLanguage(language.code), className: "justify-between gap-4", children: [_jsx("span", { dir: language.dir, children: language.name }), current === language.code && _jsx(Check, { className: "size-4" })] }, language.code)))] })] }));
}
