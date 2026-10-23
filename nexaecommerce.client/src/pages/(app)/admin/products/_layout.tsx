import { Navigate, Outlet } from 'react-router';
import { useAuth } from '@/hooks/use-auth';
import { hasPermission } from '@/lib/permissions';
import { PERM } from '@/lib/api/admin';

export default function AdminProductsLayout() {
    const { user, isLoading } = useAuth();

    if (isLoading) {
        return null;
    }

    if (!user) {
        return <Navigate to="/login" replace />;
    }

    if (!hasPermission(user.permissions, PERM.productsRead)) {
        return <Navigate to="/admin/pos" replace />;
    }

    return <Outlet />;
}