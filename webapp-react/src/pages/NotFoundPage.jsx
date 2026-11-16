import { Link } from 'react-router-dom'
import { usePageTitle } from '../hooks/usePageTitle'

export default function NotFoundPage() {
  usePageTitle('Page not found')

  return (
    <div className="not-found">
      <div className="code">404</div>
      <h1>This quest doesn't exist</h1>
      <p className="muted">The page you're looking for was moved, deleted or never existed.</p>
      <div className="button-row" style={{ justifyContent: 'center' }}>
        <Link className="btn btn-primary" to="/">
          Back to home
        </Link>
        <Link className="btn btn-secondary" to="/quests">
          Browse quests
        </Link>
      </div>
    </div>
  )
}
