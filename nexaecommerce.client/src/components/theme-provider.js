import { jsx as _jsx } from "react/jsx-runtime";
import { createContext, useContext, useEffect, useState } from 'react';
const ThemeProviderContext = createContext({
    theme: 'system',
    setTheme: () => null,
});
export function ThemeProvider({ children, defaultTheme = 'system', storageKey = 'nexaecommerce-theme', }) {
    const [theme, setThemeState] = useState(() => localStorage.getItem(storageKey) || defaultTheme);
    useEffect(() => {
        const root = window.document.documentElement;
        root.classList.remove('light', 'dark');
        const resolved = theme === 'system'
            ? window.matchMedia('(prefers-color-scheme: dark)').matches
                ? 'dark'
                : 'light'
            : theme;
        root.classList.add(resolved);
    }, [theme]);
    const setTheme = (next) => {
        localStorage.setItem(storageKey, next);
        setThemeState(next);
    };
    return (_jsx(ThemeProviderContext.Provider, { value: { theme, setTheme }, children: children }));
}
// eslint-disable-next-line react-refresh/only-export-components
export function useTheme() {
    return useContext(ThemeProviderContext);
}
