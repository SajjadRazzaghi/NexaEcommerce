import { jsx as _jsx } from "react/jsx-runtime";
import { meta } from './meta';
// Canonical route shape. Copy this folder to src/routes/(app)/{domain}/ and rename.
// Underscore-prefixed folders are ignored by the router — pure copy-source scaffolding,
// mirroring the backend's _Template rule.
//
// generouted route-module exports (replacing the Next-style separate files):
//   default export → the screen          (was page.tsx)
//   Pending        → Suspense fallback   (was loading.tsx)
//   Catch          → error boundary      (was error.tsx)
//   meta.ts        → title / permissions (plain sibling module)
export default function TemplatePage() {
    return _jsx("h1", { children: meta.title });
}
export function Pending() {
    return _jsx("p", { children: "Loading\u2026" });
}
export function Catch() {
    return _jsx("p", { children: "Something went wrong." });
}
