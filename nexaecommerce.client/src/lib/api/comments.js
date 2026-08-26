import { api } from './client';
export const commentsApi = {
    list: (entityType, entityId) => api.get(`/comments/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`),
    create: (entityType, entityId, body, url) => api.post(`/comments/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`, { body, url }),
    remove: (id) => api.del(`/comments/${id}`),
    mentionable: (q) => api.get(`/comments/mentionable?q=${encodeURIComponent(q)}`),
};
