import { useCallback, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import QuestionEditor from '../components/QuestionEditor'
import Spinner from '../components/Spinner'
import { DIFFICULTIES, DIFFICULTY_XP } from '../constants'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { courseService } from '../services/courseService'
import { questService } from '../services/questService'
import { emptyQuest, emptyQuestion, toFormState, toRequest, validateQuest } from '../utils/questForm'
import './QuestFormPage.css'

// /quests/new and /quests/:id/edit (admins only). Loads the quest (when editing)
// and the courses, then shows the form.
export default function QuestFormPage() {
  const { id } = useParams()
  const editing = id !== undefined
  usePageTitle(editing ? 'Edit quest' : 'New quest')

  const loadData = useCallback(
    () => Promise.all([editing ? questService.getForEdit(id) : null, courseService.getAll()]),
    [editing, id],
  )
  const { data, loading, error, reload } = useApiData(loadData)

  if (loading && !data) return <Spinner />
  if (!data) return <ErrorMessage error={error} onRetry={error?.status === 404 ? undefined : reload} />

  const [quest, courses] = data
  return <QuestForm questId={id} initial={quest ? toFormState(quest) : emptyQuest()} courses={courses} />
}

function QuestForm({ questId, initial, courses }) {
  const navigate = useNavigate()
  const toast = useToast()
  const [form, setForm] = useState(initial)
  const [errors, setErrors] = useState({})
  const [error, setError] = useState(null)
  const [saving, setSaving] = useState(false)

  const set = (field, value) => setForm({ ...form, [field]: value })

  const updateQuestions = (questions) => setForm({ ...form, questions })
  const changeQuestion = (index, question) => updateQuestions(form.questions.map((q, i) => (i === index ? question : q)))
  const removeQuestion = (index) => updateQuestions(form.questions.filter((_, i) => i !== index))
  const moveQuestion = (index, step) => {
    const questions = [...form.questions]
    ;[questions[index], questions[index + step]] = [questions[index + step], questions[index]]
    updateQuestions(questions)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError(null)

    const found = validateQuest(form)
    setErrors(found)
    if (Object.keys(found).length > 0) {
      toast.error('Please fix the highlighted fields.')
      return
    }

    setSaving(true)
    try {
      if (questId) {
        await questService.update(questId, toRequest(form))
        toast.success('Quest saved.')
      } else {
        await questService.create(toRequest(form))
        toast.success('Quest created.')
      }
      navigate('/quests')
    } catch (err) {
      // Validation errors from the API go next to the fields, like the client-side ones
      setErrors(err.fieldErrors ?? {})
      setError(err)
      setSaving(false)
    }
  }

  const totalPoints = form.questions.reduce((sum, q) => sum + (Number(q.points) || 0), 0)

  return (
    <div className="quest-form-page">
      <Link className="back-link" to="/quests">
        ← All quests
      </Link>
      <div className="page-header">
        <div>
          <h1>{questId ? 'Edit quest' : 'New quest'}</h1>
          <p>Students earn the XP for the difficulty the first time they complete the quest.</p>
        </div>
      </div>

      <form className="form" onSubmit={handleSubmit} noValidate>
        <ErrorMessage error={error} />

        <section className="card form">
          <FormField label="Title" htmlFor="title" error={errors.title}>
            <input
              id="title"
              className={`input ${errors.title ? 'invalid' : ''}`}
              value={form.title}
              maxLength={100}
              onChange={(e) => set('title', e.target.value)}
            />
          </FormField>
          <FormField label="Description" htmlFor="description" error={errors.description} hint="Optional, up to 500 characters.">
            <textarea
              id="description"
              className="textarea"
              value={form.description}
              maxLength={500}
              onChange={(e) => set('description', e.target.value)}
            />
          </FormField>
          <div className="form-row">
            <FormField label="Difficulty" htmlFor="difficulty" error={errors.difficulty}>
              <select
                id="difficulty"
                className={`select ${errors.difficulty ? 'invalid' : ''}`}
                value={form.difficulty}
                onChange={(e) => set('difficulty', e.target.value)}
              >
                <option value="">Choose…</option>
                {DIFFICULTIES.map((d) => (
                  <option key={d} value={d}>
                    {d} ({DIFFICULTY_XP[d]} XP)
                  </option>
                ))}
              </select>
            </FormField>
            <FormField label="Course" htmlFor="course" error={errors.courseId}>
              <select id="course" className="select" value={form.courseId} onChange={(e) => set('courseId', e.target.value)}>
                <option value="">No course</option>
                {courses.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.code} – {c.title}
                  </option>
                ))}
              </select>
            </FormField>
          </div>
        </section>

        {errors.questions && <p className="field-error">{errors.questions}</p>}

        {form.questions.map((question, index) => (
          <QuestionEditor
            key={index}
            question={question}
            index={index}
            total={form.questions.length}
            errors={errors}
            onChange={(q) => changeQuestion(index, q)}
            onRemove={() => removeQuestion(index)}
            onMove={(step) => moveQuestion(index, step)}
          />
        ))}

        <button
          type="button"
          className="btn btn-secondary"
          onClick={() => updateQuestions([...form.questions, emptyQuestion()])}
        >
          + Add question
        </button>

        <div className="form-footer">
          <span className="muted">
            {form.questions.length} {form.questions.length === 1 ? 'question' : 'questions'} · {totalPoints} points
          </span>
          <div className="button-row">
            <Link className="btn btn-ghost" to="/quests">
              Cancel
            </Link>
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Saving…' : questId ? 'Save changes' : 'Create quest'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}
