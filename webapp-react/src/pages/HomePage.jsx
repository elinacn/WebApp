import { useCallback } from 'react'
import { Link } from 'react-router-dom'
import DifficultyBadge from '../components/DifficultyBadge'
import ErrorMessage from '../components/ErrorMessage'
import Spinner from '../components/Spinner'
import WeekTracker from '../components/WeekTracker'
import { useAuth } from '../context/authContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { questService } from '../services/questService'
import { statsService } from '../services/statsService'
import './HomePage.css'

const NO_ACTIVITY = [false, false, false, false, false, false, false]

export default function HomePage() {
  usePageTitle('Home')
  const { user, loading: authLoading } = useAuth()
  const userId = user?.id

  // Everything the dashboard shows, loaded in parallel. Visitors get the leaderboard only.
  const loadDashboard = useCallback(async () => {
    const [topUsers, profile, nextQuests] = await Promise.all([
      statsService.getLeaderboard({ top: 3 }),
      userId ? statsService.getProfile() : null,
      userId ? questService.search({ status: 'notCompleted', sort: 'difficulty' }) : [],
    ])
    return { topUsers, profile, nextQuests: nextQuests.slice(0, 3) }
  }, [userId])

  const { data, loading, error, reload } = useApiData(loadDashboard)

  if (authLoading || (loading && !data)) return <Spinner />
  if (!data) return <ErrorMessage error={error} onRetry={reload} />

  const profile = data?.profile
  const streak = profile?.streak ?? 0

  return (
    <div className="home-grid">
      <div className="home-side">
        <section className="card">
          <div className="card-header">
            <h3>Weekly streak</h3>
            <span className="streak-pill">🔥 {streak}</span>
          </div>
          <p className="muted small">
            {user ? 'Complete a quest each day to keep your flame alive.' : 'Sign in to start your streak.'}
          </p>
          <WeekTracker week={profile?.week ?? NO_ACTIVITY} />
        </section>

        {user && (
          <section className="card">
            <div className="card-header">
              <h3>Up next</h3>
            </div>
            {data.nextQuests.length === 0 ? (
              <p className="muted small">You've completed every quest. Legendary! 🏆</p>
            ) : (
              <ul className="mini-list">
                {data.nextQuests.map((quest) => (
                  <li key={quest.id}>
                    <Link className="mini-item" to={`/quests/${quest.id}/play`}>
                      <span className="name">{quest.title}</span>
                      <DifficultyBadge difficulty={quest.difficulty} />
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </section>
        )}
      </div>

      <section className="card welcome">
        <span className="badge">Hit new heights with code</span>
        <h1>{user ? `Welcome back, ${user.name}!` : 'Welcome, adventurer!'}</h1>

        {user && (
          <div className="stat-row">
            <div className="stat streak">
              <span className="value">🔥 {streak}</span>
              <span className="label">Day streak</span>
            </div>
            <div className="stat xp">
              <span className="value">⚡ {profile?.user.points ?? user.points}</span>
              <span className="label">Total XP</span>
            </div>
            <div className="stat">
              <span className="value">✅ {profile?.user.quizzesCompleted ?? user.quizzesCompleted}</span>
              <span className="label">Quests done</span>
            </div>
          </div>
        )}

        <p className="tagline">
          Level up your coding skills with quests, keep your streak alive and climb the leaderboard. Master your craft
          one quest at a time!
        </p>

        <div className="invite">
          <p>{user ? 'Ready for your next challenge?' : 'Sign in to save your progress and earn XP.'}</p>
          <div className="button-row" style={{ justifyContent: 'center' }}>
            <Link className="btn btn-primary" to="/quests">
              Explore quests
            </Link>
            {!user && (
              <Link className="btn btn-secondary" to="/login">
                Sign in
              </Link>
            )}
          </div>
        </div>
      </section>

      <aside className="card">
        <div className="card-header">
          <h3>Top learners</h3>
        </div>
        {data.topUsers.length === 0 ? (
          <p className="muted small">No scores recorded yet. Be the first!</p>
        ) : (
          <ul className="mini-list">
            {data.topUsers.map((entry) => (
              <li key={entry.userId} className="mini-item">
                <span className={`rank rank-${entry.rank}`}>#{entry.rank}</span>
                <span className="name">{entry.name}</span>
                <span className="points">{entry.points} XP</span>
              </li>
            ))}
          </ul>
        )}
        <Link className="btn btn-secondary btn-block" to="/leaderboard">
          View leaderboard →
        </Link>
      </aside>
    </div>
  )
}
