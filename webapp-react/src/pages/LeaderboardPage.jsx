import { useCallback, useState } from 'react'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import Spinner from '../components/Spinner'
import { useAuth } from '../context/authContext'
import { useApiData } from '../hooks/useApiData'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { usePageTitle } from '../hooks/usePageTitle'
import { statsService } from '../services/statsService'

const MEDALS = { 1: '🥇', 2: '🥈', 3: '🥉' }
const PROGRAMS = ['Web programming', 'Programming basics']

export default function LeaderboardPage() {
  usePageTitle('Leaderboard')
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [program, setProgram] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim())

  const loadEntries = useCallback(
    () => statsService.getLeaderboard({ search: debouncedSearch, program }),
    [debouncedSearch, program],
  )
  const { data: entries, loading, error, reload } = useApiData(loadEntries)

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Leaderboard</h1>
          <p>Students ranked by XP. Complete new quests to climb!</p>
        </div>
      </div>

      <div className="card filters" style={{ gridTemplateColumns: '2fr 1fr' }} role="search">
        <FormField label="Search" htmlFor="lb-search">
          <input
            id="lb-search"
            className="input"
            type="search"
            placeholder="Find a student…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </FormField>
        <FormField label="Program" htmlFor="lb-program">
          <select id="lb-program" className="select" value={program} onChange={(e) => setProgram(e.target.value)}>
            <option value="">All programs</option>
            {PROGRAMS.map((p) => (
              <option key={p}>{p}</option>
            ))}
          </select>
        </FormField>
      </div>

      <ErrorMessage error={error} onRetry={reload} />

      {loading && !entries ? (
        <Spinner label="Loading leaderboard…" />
      ) : entries?.length === 0 ? (
        <EmptyState icon="🏆" title="No students found" />
      ) : (
        entries && (
          <div className="card table-wrap">
            <table className="table leaderboard">
              <thead>
                <tr>
                  <th>Rank</th>
                  <th>Student</th>
                  <th>Program</th>
                  <th>Quests</th>
                  <th>XP</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr key={entry.userId} className={entry.userId === user?.id ? 'me' : undefined}>
                    <td className="rank-cell">{MEDALS[entry.rank] ?? `#${entry.rank}`}</td>
                    <td>
                      <strong>{entry.name}</strong>
                      {entry.userId === user?.id && <span className="badge" style={{ marginLeft: 8 }}>You</span>}
                    </td>
                    <td className="muted">{entry.studyProgram}</td>
                    <td>{entry.quizzesCompleted}</td>
                    <td className="xp-cell">{entry.points} XP</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      )}
    </>
  )
}
