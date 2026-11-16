import { Outlet } from 'react-router-dom'
import Navbar from './Navbar'

// The frame around every page: navbar, the page itself, footer
export default function Layout() {
  return (
    <div className="app">
      <Navbar />
      <main className="app-main">
        <Outlet />
      </main>
      <footer className="site-footer">© 2026 CodeQuest · Subapp 2 (ASP.NET API + React)</footer>
    </div>
  )
}
