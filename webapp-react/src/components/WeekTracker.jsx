const DAY_LABELS = ['M', 'T', 'W', 'T', 'F', 'S', 'S']

// Monday to Sunday, with a flame on each day the user completed a quest
export default function WeekTracker({ week }) {
  return (
    <div className="week-tracker">
      {DAY_LABELS.map((label, i) => (
        <div key={i} className="day-column">
          <span className="day-label">{label}</span>
          <span className={`day-circle ${week[i] ? 'active' : ''}`} aria-label={week[i] ? 'Active' : 'Inactive'}>
            {week[i] ? '✓' : ''}
          </span>
        </div>
      ))}
    </div>
  )
}
