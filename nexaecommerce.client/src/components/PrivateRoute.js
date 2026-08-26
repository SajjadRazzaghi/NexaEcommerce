import { jsx as _jsx } from "react/jsx-runtime";
// NexaEcommerce.Client/src/components/PrivateRoute.tsx
import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
export const PrivateRoute = () => {
    const isAuthenticated = !!localStorage.getItem('accessToken');
    return isAuthenticated ? _jsx(Outlet, {}) : _jsx(Navigate, { to: "/login", replace: true });
};
