import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Fragment, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Loader2, MessagesSquare, Send, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { commentsApi } from '@/lib/api/comments';
import { isApiError } from '@/lib/problem';
import { timeAgo } from '@/lib/format';
import { cn } from '@/lib/utils';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';
/**
 * Threaded comments for any entity, keyed by `(entityType, entityId)`. Sits beside the audit timeline on
 * a record's detail page. The composer supports `@mentions` with live autocomplete — mentioned users get
 * a notification linking back here.
 */
export function Comments({ entityType, entityId }) {
    const { t } = useTranslation();
    const queryClient = useQueryClient();
    const queryKey = ['comments', entityType, entityId];
    const query = useQuery({ queryKey, queryFn: () => commentsApi.list(entityType, entityId) });
    const remove = useMutation({
        mutationFn: (id) => commentsApi.remove(id),
        onSuccess: () => {
            toast.success(t('comments.deleted'));
            queryClient.invalidateQueries({ queryKey });
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('comments.deleteError')),
    });
    return (_jsxs("div", { className: "grid gap-4", children: [_jsx(Composer, { entityType: entityType, entityId: entityId, onPosted: () => queryClient.invalidateQueries({ queryKey }) }), query.isLoading ? (_jsx(LoadingSkeleton, { rows: 3 })) : query.isError ? (_jsx(ErrorState, { error: query.error, onRetry: () => query.refetch(), message: t('comments.loadError') })) : !query.data || query.data.length === 0 ? (_jsx(EmptyState, { icon: MessagesSquare, title: t('comments.emptyTitle'), description: t('comments.emptyDesc') })) : (_jsx("ul", { className: "grid gap-4", children: query.data.map((comment) => (_jsx(CommentRow, { comment: comment, onDelete: () => remove.mutate(comment.id), deleting: remove.isPending }, comment.id))) }))] }));
}
function CommentRow({ comment, onDelete, deleting }) {
    const { t } = useTranslation();
    return (_jsxs("li", { className: "flex gap-3", children: [_jsxs(Avatar, { className: "size-8 shrink-0", children: [comment.authorAvatarUrl && _jsx(AvatarImage, { src: comment.authorAvatarUrl, alt: "" }), _jsx(AvatarFallback, { children: initials(comment.authorName) })] }), _jsxs("div", { className: "min-w-0 flex-1", children: [_jsxs("div", { className: "flex items-center gap-2", children: [_jsx("span", { className: "text-sm font-medium", children: comment.authorName }), _jsx("span", { className: "text-muted-foreground text-xs", title: new Date(comment.createdAt).toLocaleString(), children: timeAgo(comment.createdAt) }), comment.canDelete && (_jsx(Button, { variant: "ghost", size: "icon", className: "text-muted-foreground hover:text-destructive ms-auto size-7", onClick: onDelete, disabled: deleting, "aria-label": t('comments.deleteAria'), children: _jsx(Trash2, { className: "size-3.5" }) }))] }), _jsx("p", { className: "mt-1 text-sm whitespace-pre-wrap break-words", children: renderBody(comment.body) })] })] }));
}
const MENTION_BEFORE_CARET = /@([\w.-]*)$/;
function Composer({ entityType, entityId, onPosted, }) {
    const { t } = useTranslation();
    const textareaRef = useRef(null);
    const [value, setValue] = useState('');
    const [mention, setMention] = useState(null);
    const [highlight, setHighlight] = useState(0);
    const suggestions = useQuery({
        queryKey: ['comments', 'mentionable', mention?.query ?? ''],
        queryFn: () => commentsApi.mentionable(mention?.query ?? ''),
        enabled: mention !== null,
    });
    const options = mention ? suggestions.data ?? [] : [];
    const post = useMutation({
        mutationFn: (body) => commentsApi.create(entityType, entityId, body, window.location.pathname),
        onSuccess: () => {
            setValue('');
            setMention(null);
            onPosted();
        },
        onError: (error) => toast.error(isApiError(error) ? (error.problem.detail ?? error.message) : t('comments.postError')),
    });
    // Detect an in-progress @mention immediately before the caret and open the suggestion list.
    const syncMention = (text, caret) => {
        const match = text.slice(0, caret).match(MENTION_BEFORE_CARET);
        if (match) {
            setMention({ query: match[1], start: caret - match[0].length });
            setHighlight(0);
        }
        else {
            setMention(null);
        }
    };
    const onChange = (e) => {
        setValue(e.target.value);
        syncMention(e.target.value, e.target.selectionStart ?? e.target.value.length);
    };
    const insertMention = (user) => {
        if (!mention)
            return;
        const caret = textareaRef.current?.selectionStart ?? value.length;
        const next = `${value.slice(0, mention.start)}@${user.token} ${value.slice(caret)}`;
        setValue(next);
        setMention(null);
        // Restore focus and place the caret right after the inserted mention.
        requestAnimationFrame(() => {
            const pos = mention.start + user.token.length + 2;
            textareaRef.current?.focus();
            textareaRef.current?.setSelectionRange(pos, pos);
        });
    };
    const submit = () => {
        const body = value.trim();
        if (!body || post.isPending)
            return;
        post.mutate(body);
    };
    const onKeyDown = (e) => {
        if (mention && options.length > 0) {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                setHighlight((h) => (h + 1) % options.length);
                return;
            }
            if (e.key === 'ArrowUp') {
                e.preventDefault();
                setHighlight((h) => (h - 1 + options.length) % options.length);
                return;
            }
            if (e.key === 'Enter' || e.key === 'Tab') {
                e.preventDefault();
                insertMention(options[highlight]);
                return;
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                setMention(null);
                return;
            }
        }
        // Cmd/Ctrl+Enter submits from anywhere in the textarea.
        if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
            e.preventDefault();
            submit();
        }
    };
    return (_jsxs("div", { className: "relative", children: [_jsxs("div", { className: "border-input focus-within:ring-ring/50 grid gap-2 rounded-lg border p-2 focus-within:ring-[3px]", children: [_jsx("textarea", { ref: textareaRef, value: value, onChange: onChange, onKeyDown: onKeyDown, rows: 2, placeholder: t('comments.placeholder'), className: "placeholder:text-muted-foreground resize-none bg-transparent px-1 text-sm outline-none" }), _jsxs("div", { className: "flex items-center justify-between", children: [_jsx("span", { className: "text-muted-foreground text-xs", children: t('comments.sendHint') }), _jsxs(Button, { size: "sm", onClick: submit, disabled: !value.trim() || post.isPending, children: [post.isPending ? _jsx(Loader2, { className: "animate-spin" }) : _jsx(Send, { className: "size-4" }), t('comments.post')] })] })] }), mention && options.length > 0 && (_jsx("ul", { className: "bg-popover absolute z-10 mt-1 max-h-56 w-64 overflow-y-auto rounded-lg border p-1 shadow-md", children: options.map((user, i) => (_jsx("li", { children: _jsxs("button", { type: "button", 
                        // Keep the textarea focused: pick on mousedown before it blurs.
                        onMouseDown: (e) => {
                            e.preventDefault();
                            insertMention(user);
                        }, className: cn('flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-start text-sm', i === highlight ? 'bg-accent' : 'hover:bg-accent/60'), children: [_jsxs(Avatar, { className: "size-6", children: [user.avatarUrl && _jsx(AvatarImage, { src: user.avatarUrl, alt: "" }), _jsx(AvatarFallback, { className: "text-[10px]", children: initials(user.name) })] }), _jsx("span", { className: "min-w-0 flex-1 truncate", children: user.name }), _jsxs("span", { className: "text-muted-foreground truncate text-xs", children: ["@", user.token] })] }) }, user.id))) }))] }));
}
// Bold @mentions inline so they read as references rather than plain text.
function renderBody(body) {
    const parts = body.split(/(@[\w.-]+)/g);
    return parts.map((part, i) => part.startsWith('@') ? (_jsx("span", { className: "text-primary font-medium", children: part }, i)) : (_jsx(Fragment, { children: part }, i)));
}
function initials(value) {
    const parts = value.trim().split(/\s+/);
    if (parts.length >= 2)
        return (parts[0][0] + parts[1][0]).toUpperCase();
    return value.slice(0, 2).toUpperCase();
}
