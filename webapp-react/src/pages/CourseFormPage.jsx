import { useCallback, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import Spinner from '../components/Spinner'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { courseService } from '../services/courseService'

const EMPTY_COURSE = { code: '', title: '', description: '' }

// Same rules as CourseFormDto in the API
function validate(form) {
  const errors = {}
  const code = form.code.trim()
  if (!code) errors.code = 'Course code is required.'
  else if (code.length > 20) errors.code = 'Course code can be at most 20 characters.'
  else if (!/^[A-Za-z0-9\- ]+$/.test(code)) errors.code = 'Use only letters, numbers, spaces and dashes.'

  if (!form.title.trim()) errors.title = 'Course title is required.'
  else if (form.title.trim().length > 100) errors.title = 'Course title can be at most 100 characters.'

  if (form.description.length > 500) errors.description = 'Description can be at most 500 characters.'
  return errors
}

// /courses/new and /courses/:id/edit (admins only)
export default function CourseFormPage() {
  const { id } = useParams()
  usePageTitle(id ? 'Edit course' : 'New course')

  const loadCourse = useCallback(() => (id ? courseService.get(id) : Promise.resolve(null)), [id])
  const { data, loading, error, reload } = useApiData(loadCourse)

  if (id && loading && !data) return <Spinner />
  if (id && !data) return <ErrorMessage error={error} onRetry={error?.status === 404 ? undefined : reload} />

  const initial = data
    ? { code: data.code, title: data.title, description: data.description ?? '' }
    : EMPTY_COURSE
  return <CourseForm key={id ?? 'new'} courseId={id} initial={initial} />
}

function CourseForm({ courseId, initial }) {
  const navigate = useNavigate()
  const toast = useToast()
  const [form, setForm] = useState(initial)
  const [errors, setErrors] = useState({})
  const [error, setError] = useState(null)
  const [saving, setSaving] = useState(false)

  const update = (field) => (event) => {
    setForm({ ...form, [field]: event.target.value })
    setErrors({ ...errors, [field]: undefined })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const found = validate(form)
    setErrors(found)
    setError(null)
    if (Object.keys(found).length > 0) return

    setSaving(true)
    try {
      if (courseId) {
        await courseService.update(courseId, form)
        toast.success('Course saved.')
        navigate(`/courses/${courseId}`)
      } else {
        const created = await courseService.create(form)
        toast.success(`${created.code} was created.`)
        navigate(`/courses/${created.id}`)
      }
    } catch (err) {
      setErrors(err.fieldErrors ?? {})
      setError(err)
      setSaving(false)
    }
  }

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      <Link className="back-link" to={courseId ? `/courses/${courseId}` : '/courses'}>
        ← Back
      </Link>
      <div className="page-header">
        <h1>{courseId ? 'Edit course' : 'New course'}</h1>
      </div>

      <form className="card form" onSubmit={handleSubmit} noValidate>
        <ErrorMessage error={error} />
        <FormField label="Code" htmlFor="code" error={errors.code} hint="E.g. WEB-101. Stored in upper case.">
          <input
            id="code"
            className={`input ${errors.code ? 'invalid' : ''}`}
            value={form.code}
            maxLength={20}
            onChange={update('code')}
          />
        </FormField>
        <FormField label="Title" htmlFor="course-title" error={errors.title}>
          <input
            id="course-title"
            className={`input ${errors.title ? 'invalid' : ''}`}
            value={form.title}
            maxLength={100}
            onChange={update('title')}
          />
        </FormField>
        <FormField label="Description" htmlFor="course-description" error={errors.description} hint="Optional.">
          <textarea
            id="course-description"
            className="textarea"
            value={form.description}
            maxLength={500}
            onChange={update('description')}
          />
        </FormField>
        <div className="button-row">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Saving…' : courseId ? 'Save changes' : 'Create course'}
          </button>
          <Link className="btn btn-ghost" to={courseId ? `/courses/${courseId}` : '/courses'}>
            Cancel
          </Link>
        </div>
      </form>
    </div>
  )
}
