import { useEffect } from 'react'
import { BrowserRouter, HashRouter, Routes, Route, Navigate } from 'react-router-dom'
import { config } from '../config/env'
import { useAuthStore } from '../modules/auth/store/auth-store'
import { LoginPage } from '../modules/auth/pages/LoginPage'
import { RegisterPage } from '../modules/auth/pages/RegisterPage'
import { ProtectedRoute } from '../modules/auth/components/ProtectedRoute'
import { ProtectedRouteByPermission } from '../modules/authorization/components/ProtectedRouteByPermission'
import { AppLayout } from './components/AppLayout'
import { RequireFullContext } from './components/RequireFullContext'
import { HomePage } from './pages/HomePage'
import { ForbiddenPage } from './pages/ForbiddenPage'
import { PublishPage } from './pages/PublishPage'
import { ContentListPage } from './pages/ContentListPage'
import { EditContentPage } from './pages/EditContentPage'
import { SearchPage } from './pages/SearchPage'
import { SitesPage } from './pages/SitesPage'
import { RolesPermissionsPage } from './pages/RolesPermissionsPage'
import { SchemaDesignerPage } from './pages/SchemaDesignerPage'
import { HierarchiesPage } from './pages/HierarchiesPage'
import { TagsPage } from './pages/TagsPage'
import { UsersPage } from './pages/UsersPage'

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forbidden" element={<ForbiddenPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout>
              <HomePage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/content"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <ContentListPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route path="/content/new" element={<Navigate to="/content" replace />} />
      <Route
        path="/content/:contentId/edit"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <EditContentPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <HomePage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/publish"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <ProtectedRouteByPermission permission="content.publish" showForbidden>
                <AppLayout>
                  <PublishPage />
                </AppLayout>
              </ProtectedRouteByPermission>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/search"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <SearchPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/sites"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <SitesPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/roles"
        element={
          <ProtectedRoute>
            <AppLayout>
              <RolesPermissionsPage />
            </AppLayout>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/users"
        element={
          <ProtectedRoute>
            <ProtectedRouteByPermission permission="user.list" showForbidden>
              <AppLayout>
                <UsersPage />
              </AppLayout>
            </ProtectedRouteByPermission>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/schemas"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <SchemaDesignerPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/hierarchies"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <HierarchiesPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route
        path="/admin/tags"
        element={
          <ProtectedRoute>
            <RequireFullContext>
              <AppLayout>
                <TagsPage />
              </AppLayout>
            </RequireFullContext>
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

const Router = config.routerType === 'hash' ? HashRouter : BrowserRouter

export default function App() {
  const rehydrate = useAuthStore((s) => s.rehydrate)

  useEffect(() => {
    rehydrate()
  }, [rehydrate])

  return (
    <Router>
      <AppRoutes />
    </Router>
  )
}
