import { jsx as _jsx } from "react/jsx-runtime";
import { Toaster as Sonner } from 'sonner';
import { useTheme } from '@/components/theme-provider';
// Sonner toaster wired to NexaECommerce's ThemeProvider (not next-themes).
export function Toaster(props) {
    const { theme } = useTheme();
    return (_jsx(Sonner, { theme: theme, className: "toaster group", richColors: true, ...props }));
}
