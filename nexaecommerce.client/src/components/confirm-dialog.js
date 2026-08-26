import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, } from '@/components/ui/dialog';
/**
 * Controlled confirmation dialog for destructive or irreversible actions. The caller owns `open`
 * and `pending`; `onConfirm` fires the action (the dialog stays open while pending so errors can
 * surface via toast without a flash of the closed state).
 */
export function ConfirmDialog({ open, onOpenChange, title, description, confirmLabel, destructive = false, pending = false, onConfirm, }) {
    const { t } = useTranslation();
    return (_jsx(Dialog, { open: open, onOpenChange: onOpenChange, children: _jsxs(DialogContent, { className: "sm:max-w-md", children: [_jsxs(DialogHeader, { children: [_jsx(DialogTitle, { children: title }), _jsx(DialogDescription, { children: description })] }), _jsxs(DialogFooter, { children: [_jsx(DialogClose, { asChild: true, children: _jsx(Button, { variant: "outline", disabled: pending, children: t('common.cancel') }) }), _jsxs(Button, { variant: destructive ? 'destructive' : 'default', onClick: onConfirm, disabled: pending, children: [pending && _jsx(Loader2, { className: "animate-spin" }), confirmLabel ?? t('common.confirm')] })] })] }) }));
}
