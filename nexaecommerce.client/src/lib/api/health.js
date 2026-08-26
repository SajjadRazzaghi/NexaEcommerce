import { api } from './client';
/** Permission gating the health dashboard — matches Features/Health/Permissions.cs. */
export const HEALTH_PERM = { read: 'health.read' };
export const healthApi = {
    get: () => api.get('/health/'),
};
