import { useEffect, useRef, useState } from 'react'
import { Link, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'

export default function Navbar() {
  const { user, isAdmin, logout } = useAuth()
  const toast = useToast()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false) // mobile navigation
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const userMenuRef = useRef(null)

  // Close the user menu when clicking anywhere else
  useEffect(() => {
    if (!userMenuOpen) return
    const onClick = (event) => {
      if (!userMenuRef.current?.contains(event.target)) setUserMenuOpen(false)
    }
    document.addEventListener('mousedown', onClick)
    return () => document.removeEventListener('mousedown', onClick)
  }, [userMenuOpen])

  const closeMenus = () => {
    setMenuOpen(false)
    setUserMenuOpen(false)
  }

  const handleLogout = async () => {
    closeMenus()
    await logout()
    toast.success('You are signed out.')
    navigate('/')
  }

  return (
    <header className={`navbar ${menuOpen ? 'open' : ''}`}>
      <nav className="navbar-inner" aria-label="Main">
        <Link className="navbar-logo" to="/" onClick={closeMenus}>
          <img className="navbar-logo-img" src="/images/Logo.png" alt="" />
          CodeQuest
        </Link>

        <button
          type="button"
          className="navbar-toggle"
          aria-label="Toggle navigation"
          aria-expanded={menuOpen}
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? '✕' : '☰'}
        </button>

        <ul className="navbar-links">
          <li>
            <NavLink to="/" end onClick={closeMenus}>
              Home
            </NavLink>
          </li>
          <li>
            <NavLink to="/quests" onClick={closeMenus}>
              Quests
            </NavLink>
          </li>
          <li>
            <NavLink to="/leaderboard" onClick={closeMenus}>
              Leaderboard
            </NavLink>
          </li>
          <li>
            <NavLink to="/courses" onClick={closeMenus}>
              Courses
            </NavLink>
          </li>
          {isAdmin && (
            <li>
              <NavLink to="/users" onClick={closeMenus}>
                Users
              </NavLink>
            </li>
          )}
        </ul>

        <div className="navbar-user">
          {user ? (
            <div className="user-menu" ref={userMenuRef}>
              <button
                type="button"
                className="user-menu-button"
                aria-haspopup="menu"
                aria-expanded={userMenuOpen}
                onClick={() => setUserMenuOpen((open) => !open)}
              >
                <img className="navbar-avatar" src="/images/default-profile.svg" alt="" />
                <span>
                  {user.name} · {user.points} XP
                </span>
              </button>
              {userMenuOpen && (
                <div className="user-menu-panel" role="menu">
                  <p>
                    Signed in as <strong>{user.username}</strong>
                    {isAdmin && <span className="badge badge-admin"> Admin</span>}
                  </p>
                  <Link to="/profile" role="menuitem" onClick={closeMenus}>
                    My profile
                  </Link>
                  <button type="button" role="menuitem" className="danger" onClick={handleLogout}>
                    Sign out
                  </button>
                </div>
              )}
            </div>
          ) : (
            <Link className="btn btn-secondary btn-sm" to="/login" onClick={closeMenus}>
              Sign in
            </Link>
          )}
        </div>
      </nav>
    </header>
  )
}
