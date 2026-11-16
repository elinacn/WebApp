import { createContext, useContext } from 'react'

export const AuthContext = createContext(null)

// { user, loading, isAdmin, login, logout, refreshUser } – see AuthProvider
export function useAuth() {
  return useContext(AuthContext)
}
