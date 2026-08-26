import { api } from './client';
/** Permission strings the admin UI gates on — must match the backend slice constants
 * (Features/Roles/Permissions.cs, Features/Users/Permissions.cs). */
export const PERM = {
    usersRead: 'users.read',
    usersCreate: 'users.create',
    usersUpdate: 'users.update',
    usersDelete: 'users.delete',
    rolesRead: 'roles.read',
    rolesCreate: 'roles.create',
    rolesUpdate: 'roles.update',
    rolesDelete: 'roles.delete',
    settingsRead: 'settings.read',
    settingsUpdate: 'settings.update',
    auditRead: 'audit.read',
    webhooksRead: 'webhooks.read',
    webhooksCreate: 'webhooks.create',
    webhooksUpdate: 'webhooks.update',
    webhooksDelete: 'webhooks.delete',
    brandsRead: 'brands.read',
    brandsCreate: 'brands.create',
    brandsUpdate: 'brands.update',
    brandsDelete: 'brands.delete',
    brandsRestore: 'brands.restore',
    brandsStatus: 'brands.status',
    brandsPublish: 'brands.publish',
    brandsFeature: 'brands.feature',
};
export const permissionsApi = {
    catalog: () => api.get('/permissions'),
};
export const rolesApi = {
    list: () => api.get('/roles/'),
    create: (body) => api.post('/roles/', body),
    update: (id, body) => api.put(`/roles/${id}`, body),
    remove: (id) => api.del(`/roles/${id}`),
};
export const settingsApi = {
    list: () => api.get('/settings/'),
    update: (key, value) => api.put(`/settings/${key}`, { value }),
};
export const usersApi = {
    list: (search) => api.get(`/users/${search ? `?search=${encodeURIComponent(search)}` : ''}`),
    create: (body) => api.post('/users/', body),
    update: (id, body) => api.put(`/users/${id}`, body),
    updateRoles: (id, roles) => api.put(`/users/${id}/roles`, { roles }),
    confirmEmail: (id) => api.post(`/users/${id}/confirm-email`),
    resendConfirmation: (id) => api.post(`/users/${id}/resend-confirmation`),
    sendPasswordReset: (id) => api.post(`/users/${id}/send-password-reset`),
    disableTwoFactor: (id) => api.post(`/users/${id}/disable-2fa`),
    lock: (id) => api.post(`/users/${id}/lock`),
    unlock: (id) => api.post(`/users/${id}/unlock`),
    remove: (id) => api.del(`/users/${id}`),
};
