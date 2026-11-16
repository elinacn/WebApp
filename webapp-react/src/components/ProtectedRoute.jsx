import { Link, Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/authContext'
import EmptyState from './EmptyState'
import Spinner from './Spinner'

// Wraps pages that need a signed-in user (and optionally an admin).
// Visitors are sent to the sign-in page and brought back here afterwards.
export default function ProtectedRoute({ adminOnly = false, children }) {
  const { user, loading, isAdmin } = useAuth()
  const location = useLocation()

  if (loading) return <Spinner label="Checking your session…" />

  if (!user) return <Navigate to="/login" replace state={{ from: location }} />

  if (adminOnly && !isAdmin) {
    return (
      <EmptyState icon="🔒" title="Admins only">
        <p>You need an admin account to open this page.</p>
        <Link className="btn btn-secondary" to="/">
          Back to home
        </Link>
      </EmptyState>
    )
  }

  return children
}
