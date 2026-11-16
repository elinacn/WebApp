import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import QuestionView from './QuestionView'

// perfect / high (at least half) / low / none – picks the message and the sound
function getOutcome(score, maxScore) {
  if (maxScore <= 0 || score <= 0) return 'none'
  if (score >= maxScore) return 'perfect'
  return score * 2 >= maxScore ? 'high' : 'low'
}

const MESSAGES = {
  perfect: 'Flawless! Every answer correct. 🏆',
  high: 'Great work! You passed the quest. 🎉',
  low: 'Not bad – review the answers below and try again. 💪',
  none: "Tough one! Check the explanations and give it another go. 📚",
}

export default function QuizResult({ quest, answers, result, onRetry }) {
  const outcome = getOutcome(result.score, result.maxScore)
  const percent = result.maxScore > 0 ? Math.round((result.score / result.maxScore) * 100) : 0
  const resultsById = Object.fromEntries(result.questions.map((r) => [r.questionId, r]))
  const correctCount = result.questions.filter((r) => r.isCorrect).length

  // Same sound effects as subapp 1. Browsers may block audio; that's fine.
  useEffect(() => {
    new Audio(`/sounds/quest-${outcome}.wav`).play().catch(() => {})
  }, [outcome])

  return (
    <>
      <section className={`card result-summary ${outcome}`} role="status">
        <div className="score-ring" style={{ '--percent': percent }}>
          <span>{percent}%</span>
        </div>
        <div className="result-text">
          <h2>{MESSAGES[outcome]}</h2>
          <p>
            You scored <strong>{result.score}</strong> of <strong>{result.maxScore}</strong> points ({correctCount} of{' '}
            {result.questions.length} questions correct).
          </p>

          {!result.saved ? (
            <p className="alert alert-warning">Your answers were checked, but the result couldn't be saved. Please try again.</p>
          ) : result.xpAwarded > 0 ? (
            <p className="xp-gain">
              +{result.xpAwarded} XP! You now have {result.totalXp} XP in total.
            </p>
          ) : (
            <p className="muted">
              You already earned the XP for this quest. Your best score is {result.bestScore} of {result.maxScore}.
              Retakes don't award extra XP.
            </p>
          )}

          <div className="button-row">
            <button type="button" className="btn btn-primary" onClick={onRetry}>
              ↻ Try again
            </button>
            <Link className="btn btn-secondary" to="/quests">
              Back to quests
            </Link>
          </div>
        </div>
      </section>

      <h2 className="review-heading">Review your answers</h2>
      {quest.questions.map((question, index) => {
        const questionResult = resultsById[question.id]
        return (
          <section key={question.id} className={`card review-card ${questionResult.isCorrect ? 'correct' : 'wrong'}`}>
            <div className="card-header">
              <h3>Question {index + 1}</h3>
              <span className={`verdict ${questionResult.isCorrect ? 'correct' : 'wrong'}`}>
                {questionResult.isCorrect ? '✓ Correct' : '✗ Incorrect'} · {question.points} pt
              </span>
            </div>
            <QuestionView question={question} answer={answers[question.id]} result={questionResult} />
          </section>
        )
      })}
    </>
  )
}
