import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useFormContext } from 'react-hook-form';
import { Input } from '@/components/ui/input';
import { FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage, } from '@/components/ui/form';
/**
 * Form field primitive (§7.2): label + required marker + control + hint + inline validation message,
 * wired to the RHF form in context — no `control` prop to thread. Renders a text `<Input>` by
 * default; pass a render-child for any other control. Errors come from the resolver and from
 * `useSubmitForm` mapping server `ProblemDetails` onto fields.
 */
export function Field({ name, label, description, required, children, ...inputProps }) {
    const { control } = useFormContext();
    return (_jsx(FormField, { control: control, name: name, render: ({ field }) => (_jsxs(FormItem, { children: [label && (_jsxs(FormLabel, { children: [label, required && _jsx("span", { className: "text-destructive", children: " *" })] })), _jsx(FormControl, { children: children ? children(field) : _jsx(Input, { ...inputProps, ...field, value: field.value ?? '' }) }), description && _jsx(FormDescription, { children: description }), _jsx(FormMessage, {})] })) }));
}
