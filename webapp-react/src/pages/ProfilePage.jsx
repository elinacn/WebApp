import { useCallback } from 'react'
import { Link } from 'react-router-dom'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import Spinner from '../components/Spinner'
import WeekTracker from '../components/WeekTracker'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { statsService } from '../services/statsService'
import './HomePage.css'

const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' })

export default function ProfilePage() {
  usePageTitle('My profile')
  const loadProfile = useCallback(() => statsService.getProfile(), [])
  const { data: profile, loading, error, reload } = useApiData(loadProfile)

  if (loading && !profile) return <Spinner />
  if (!profile) return <ErrorMessage error={error} onRetry={reload} />

  const { user, streak, week, recentActivity } = profile

  return (
    <>
      <div className="page-header">
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <img src="/images/default-profile.svg" alt="" width="72" height="72" style={{ borderRadius: '50%' }} />
          <div>
            <h1>{user.name}</h1>
            <p>
              @{user.username} · {user.studyProgram}
              {user.role === 'Admin' && <span className="badge badge-admin" style={{ marginLeft: 8 }}>Admin</span>}
            </p>
          </div>
        </div>
      </div>

      <div className="stat-row" style={{ justifyContent: 'flex-start' }}>
        <div className="stat xp">
          <span className="value">⚡ {user.points}</span>
          <span className="label">Total XP</span>
        </div>
        <div className="stat">
          <span className="value">✅ {user.quizzesCompleted}</span>
          <span className="label">Quests completed</span>
        </div>
        <div className="stat streak">
          <span className="value">🔥 {streak}</span>
          <span className="label">Day streak</span>
        </div>
      </div>

      <section className="card" style={{ marginBottom: 24, maxWidth: 420 }}>
        <div className="card-header">
          <h3>This week</h3>
        </div>
        <WeekTracker week={week} />
      </section>

      <h2>Recent quests</h2>
      {recentActivity.length === 0 ? (
        <EmptyState icon="🗺️" title="No quests completed yet">
          <Link className="btn btn-primary" to="/quests">
            Start your first quest
          </Link>
        </EmptyState>
      ) : (
        <div className="card table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Quest</th>
                <th>Best score</th>
                <th>Attempts</th>
                <th>Last played</th>
                <th>
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {recentActivity.map((activity) => {
                const percent = activity.maxScore > 0 ? Math.round((activity.bestScore / activity.maxScore) * 100) : 100
                return (
                  <tr key={activity.questId}>
                    <td>
                      <strong>{activity.questTitle}</strong>
                    </td>
                    <td>
                      {activity.bestScore}/{activity.maxScore}{' '}
                      <span className={percent === 100 ? 'xp-cell' : 'muted'}>({percent}%)</span>
                    </td>
                    <td>{activity.attempts}</td>
                    <td className="muted">{dateFormat.format(new Date(activity.lastCompletedAt))}</td>
                    <td className="actions">
                      <Link className="btn btn-secondary btn-sm" to={`/quests/${activity.questId}/play`}>
                        {percent === 100 ? 'Play again' : 'Improve score'}
                      </Link>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
