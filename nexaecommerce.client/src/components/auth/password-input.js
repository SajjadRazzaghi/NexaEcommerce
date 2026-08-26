import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import * as React from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Input } from '@/components/ui/input';
/** Password field with a show/hide toggle. The toggle is keyboard-reachable and labelled. */
function PasswordInput({ className, ...props }) {
    const [visible, setVisible] = React.useState(false);
    return (_jsxs("div", { className: "relative", children: [_jsx(Input, { type: visible ? 'text' : 'password', className: cn('pe-9', className), ...props }), _jsx("button", { type: "button", onClick: () => setVisible((v) => !v), className: "text-muted-foreground hover:text-foreground focus-visible:ring-ring/50 absolute end-0 top-0 grid h-9 w-9 place-items-center rounded-md outline-none focus-visible:ring-[3px]", "aria-label": visible ? 'Hide password' : 'Show password', tabIndex: -1, children: visible ? _jsx(EyeOff, { className: "size-4" }) : _jsx(Eye, { className: "size-4" }) })] }));
}
export { PasswordInput };
