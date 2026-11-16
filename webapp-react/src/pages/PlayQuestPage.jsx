import { useCallback, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import DifficultyBadge from '../components/DifficultyBadge'
import ErrorMessage from '../components/ErrorMessage'
import QuestionView from '../components/QuestionView'
import QuizResult from '../components/QuizResult'
import Spinner from '../components/Spinner'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { questService } from '../services/questService'
import './PlayQuestPage.css'

// A question counts as answered once an option is picked or some text is typed
function isAnswered(answer) {
  return Boolean(answer?.text?.trim()) || (answer?.selected?.length ?? 0) > 0
}

export default function PlayQuestPage() {
  const { id } = useParams()
  const { refreshUser } = useAuth()
  const toast = useToast()

  const loadQuest = useCallback(() => questService.getForPlay(id), [id])
  const { data: quest, loading, error, reload } = useApiData(loadQuest)
  usePageTitle(quest?.title ?? 'Quest')

  const [answers, setAnswers] = useState({}) // { [questionId]: { text, selected } }
  const [current, setCurrent] = useState(0)
  const [result, setResult] = useState(null)
  const [submitting, setSubmitting] = useState(false)
  const [confirmingSubmit, setConfirmingSubmit] = useState(false)

  if (loading && !quest) return <Spinner label="Loading quest…" />

  if (!quest) {
    return (
      <div className="play-page">
        <Link className="back-link" to="/quests">
          ← All quests
        </Link>
        <ErrorMessage error={error} onRetry={error?.status === 404 ? undefined : reload} />
      </div>
    )
  }

  const questions = quest.questions
  const question = questions[current]
  const answeredCount = questions.filter((q) => isAnswered(answers[q.id])).length
  const unanswered = questions.length - answeredCount
  const isLast = current === questions.length - 1

  const submit = async () => {
    setConfirmingSubmit(false)
    setSubmitting(true)
    try {
      const payload = questions.map((q) => ({
        questionId: q.id,
        text: answers[q.id]?.text ?? null,
        selected: answers[q.id]?.selected ?? [],
      }))
      setResult(await questService.submit(quest.id, payload))
      window.scrollTo({ top: 0, behavior: 'smooth' })
      refreshUser() // updates the XP shown in the navbar
    } catch (err) {
      toast.error(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  // Ask first if some questions are still empty
  const handleSubmit = () => (unanswered > 0 ? setConfirmingSubmit(true) : submit())

  const retry = () => {
    setAnswers({})
    setResult(null)
    setCurrent(0)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  return (
    <div className="play-page">
      <Link className="back-link" to="/quests">
        ← All quests
      </Link>

      <header className="play-header">
        <h1>{quest.title}</h1>
        <div className="meta">
          <DifficultyBadge difficulty={quest.difficulty} />
          <span>{questions.length} questions</span>
          <span>⚡ {quest.xp} XP on first completion</span>
        </div>
        {quest.description && <p className="muted">{quest.description}</p>}
      </header>

      {result ? (
        <QuizResult quest={quest} answers={answers} result={result} onRetry={retry} />
      ) : (
        <>
          <div className="play-progress">
            <div className="labels">
              <span>
                Question {current + 1} of {questions.length}
              </span>
              <span>{answeredCount} answered</span>
            </div>
            <div className="progress">
              <div className="progress-bar" style={{ width: `${((current + 1) / questions.length) * 100}%` }} />
            </div>
            <div className="question-dots" aria-label="Jump to question">
              {questions.map((q, index) => (
                <button
                  key={q.id}
                  type="button"
                  className={`question-dot ${isAnswered(answers[q.id]) ? 'answered' : ''} ${index === current ? 'current' : ''}`}
                  onClick={() => setCurrent(index)}
                  aria-label={`Question ${index + 1}${isAnswered(answers[q.id]) ? ' (answered)' : ''}`}
                  aria-current={index === current ? 'step' : undefined}
                >
                  {index + 1}
                </button>
              ))}
            </div>
          </div>

          <section className="card">
            <div className="card-header">
              <h3>Question {current + 1}</h3>
              <span className="muted small">
                {question.points} {question.points === 1 ? 'point' : 'points'}
              </span>
            </div>
            <QuestionView
              key={question.id}
              question={question}
              answer={answers[question.id]}
              onChange={(answer) => setAnswers({ ...answers, [question.id]: answer })}
            />
          </section>

          <div className="play-nav">
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setCurrent(current - 1)}
              disabled={current === 0}
            >
              ← Previous
            </button>
            {isLast ? (
              <button type="button" className="btn btn-primary" onClick={handleSubmit} disabled={submitting}>
                {submitting ? 'Checking…' : 'Submit answers ✓'}
              </button>
            ) : (
              <button type="button" className="btn btn-primary" onClick={() => setCurrent(current + 1)}>
                Next →
              </button>
            )}
          </div>
        </>
      )}

      {confirmingSubmit && (
        <ConfirmDialog
          title="Submit with unanswered questions?"
          message={`You haven't answered ${unanswered} of ${questions.length} questions. Unanswered questions count as wrong.`}
          confirmLabel="Submit anyway"
          danger={false}
          onConfirm={submit}
          onCancel={() => setConfirmingSubmit(false)}
        />
      )}
    </div>
  )
}
