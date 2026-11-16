export default function DifficultyBadge({ difficulty }) {
  return <span className={`badge badge-${difficulty.toLowerCase()}`}>{difficulty}</span>
}
