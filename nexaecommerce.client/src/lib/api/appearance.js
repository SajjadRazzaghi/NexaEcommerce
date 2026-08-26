import { api } from './client';
/** Permission gating brand-colour changes — matches Features/Appearance/Permissions.cs. */
export const APPEARANCE_PERM = { manage: 'appearance.manage' };
export const appearanceApi = {
    get: () => api.get('/appearance/'),
    update: (body) => api.put('/appearance/', body),
};
