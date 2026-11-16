import { DIFFICULTIES } from '../constants'
import FormField from './FormField'

// Search and filter bar for the quest list. `filters` holds the current values,
// `onChange(name, value)` updates one of them, `onReset` clears them all.
export default function QuestFilters({ filters, courses, signedIn, onChange, onReset }) {
  const change = (name) => (event) => onChange(name, event.target.value)

  return (
    <div className="card filters" role="search">
      <FormField label="Search" htmlFor="quest-search">
        <input
          id="quest-search"
          className="input"
          type="search"
          placeholder="Title or description…"
          value={filters.search}
          onChange={change('search')}
        />
      </FormField>

      <FormField label="Difficulty" htmlFor="quest-difficulty">
        <select id="quest-difficulty" className="select" value={filters.difficulty} onChange={change('difficulty')}>
          <option value="">All</option>
          {DIFFICULTIES.map((d) => (
            <option key={d}>{d}</option>
          ))}
        </select>
      </FormField>

      <FormField label="Course" htmlFor="quest-course">
        <select id="quest-course" className="select" value={filters.courseId} onChange={change('courseId')}>
          <option value="">All courses</option>
          {courses.map((c) => (
            <option key={c.id} value={c.id}>
              {c.code} – {c.title}
            </option>
          ))}
        </select>
      </FormField>

      <FormField label="Progress" htmlFor="quest-status">
        <select
          id="quest-status"
          className="select"
          value={filters.status}
          onChange={change('status')}
          disabled={!signedIn}
          title={signedIn ? undefined : 'Sign in to filter by progress'}
        >
          <option value="">All quests</option>
          <option value="notCompleted">Not completed</option>
          <option value="completed">Completed</option>
        </select>
      </FormField>

      <FormField label="Sort by" htmlFor="quest-sort">
        <select id="quest-sort" className="select" value={filters.sort} onChange={change('sort')}>
          <option value="newest">Newest</option>
          <option value="title">Title (A–Z)</option>
          <option value="difficulty">Easiest first</option>
          <option value="xp">Most XP</option>
        </select>
      </FormField>

      <button type="button" className="btn btn-ghost" onClick={onReset}>
        Reset
      </button>
    </div>
  )
}
