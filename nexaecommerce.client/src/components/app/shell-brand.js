import { jsx as _jsx } from "react/jsx-runtime";
import { Brand } from '@/components/brand';
/**
 * The brand lockup in the app shell. White-label: when the user is in a non-default tenant (multi-tenant
 * mode), it shows that tenant's name + logo instead of the product brand — the accent already re-tints via
 * the appearance applier. The default tenant (and single-tenant editions) keep the product brand.
 */
export function ShellBrand({ className, markOnly }) {
    let name;
    let logoUrl;
    return _jsx(Brand, { className: className, markOnly: markOnly, name: name, logoUrl: logoUrl });
}
