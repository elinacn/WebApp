import { Link } from 'react-router-dom'
import DifficultyBadge from './DifficultyBadge'

// One quest in the list. Shows the user's best score once completed,
// and edit/delete buttons for admins.
export default function QuestCard({ quest, signedIn, isAdmin, onDelete }) {
  const completed = quest.bestPercent !== null

  return (
    <article className={`card quest-card ${completed ? 'completed' : ''}`}>
      <div className="quest-card-top">
        <DifficultyBadge difficulty={quest.difficulty} />
        {quest.courseCode && <span className="muted small">{quest.courseCode}</span>}
      </div>

      <h3>{quest.title}</h3>
      {quest.description && <p className="description">{quest.description}</p>}

      <div className="quest-meta">
        <span>📝 {quest.questionCount} questions</span>
        <span>🎯 {quest.totalPoints} points</span>
        <span className="xp">⚡ {quest.xp} XP</span>
      </div>

      {completed && (
        <div>
          <div className="progress-label">
            <span>✓ Completed</span>
            <span>Best: {quest.bestPercent}%</span>
          </div>
          <div className="progress" aria-hidden="true">
            <div className="progress-bar" style={{ width: `${quest.bestPercent}%` }} />
          </div>
        </div>
      )}

      <div className="quest-card-actions">
        {quest.questionCount === 0 ? (
          <span className="muted small">No questions yet</span>
        ) : (
          <Link
            className="btn btn-primary"
            to={signedIn ? `/quests/${quest.id}/play` : '/login'}
            state={signedIn ? undefined : { from: { pathname: `/quests/${quest.id}/play` } }}
          >
            {!signedIn ? 'Sign in to play' : completed ? 'Play again' : 'Start quest'}
          </Link>
        )}
        {isAdmin && (
          <>
            <Link className="btn btn-secondary" to={`/quests/${quest.id}/edit`} aria-label={`Edit ${quest.title}`}>
              Edit
            </Link>
            <button
              type="button"
              className="btn btn-ghost"
              onClick={() => onDelete(quest)}
              aria-label={`Delete ${quest.title}`}
            >
              🗑
            </button>
          </>
        )}
      </div>
    </article>
  )
}
