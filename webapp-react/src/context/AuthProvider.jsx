import { useCallback, useEffect, useMemo, useState } from 'react'
import { authService } from '../services/authService'
import { setUnauthorizedHandler } from '../services/httpClient'
import { AuthContext } from './authContext'

// Keeps track of who is signed in. The login itself is an HttpOnly cookie set by the
// API, so React only stores the user's details for showing them and for hiding
// buttons; the API still checks every request.
export default function AuthProvider({ children }) {
  const [user, setUser] = useState(null)
  const [loading, setLoading] = useState(true)

  // On page load: ask the API whether the cookie from an earlier visit is still valid
  useEffect(() => {
    authService
      .me()
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  // If any request says "not signed in" (e.g. the cookie expired), forget the user
  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
  }, [])

  const login = useCallback(async (username, password) => {
    const signedIn = await authService.login(username, password)
    setUser(signedIn)
    return signedIn
  }, [])

  const logout = useCallback(async () => {
    try {
      await authService.logout()
    } finally {
      setUser(null)
    }
  }, [])

  // Reloads the user, e.g. to update the XP in the navbar after finishing a quest
  const refreshUser = useCallback(() => authService.me().then(setUser).catch(() => setUser(null)), [])

  const value = useMemo(
    () => ({ user, loading, isAdmin: user?.role === 'Admin', login, logout, refreshUser }),
    [user, loading, login, logout, refreshUser],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
