# Brands route fix

The client uses Generouted file-based routing from `src/pages`.
The correct route files are:

- `src/pages/(app)/admin/brands/index.tsx` -> `/admin/brands`
- `src/pages/(app)/admin/brands/new.tsx` -> `/admin/brands/new`
- `src/pages/(app)/admin/brands/[id]/index.tsx` -> `/admin/brands/:id`
- `src/pages/(app)/admin/brands/[id]/edit.tsx` -> `/admin/brands/:id/edit`

`src/app/routes/index.tsx` was an unused second routing implementation and has been removed.

The dev script now uses `vite --force` so Vite regenerates its cached route modules after route changes.

The `BrandForm` component remains at:
`src/modules/catalog/brands/components/BrandForm.tsx`

Do not put `<BrandForm />` recursively inside the route page.
