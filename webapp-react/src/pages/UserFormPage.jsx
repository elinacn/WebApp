import { useCallback, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import Spinner from '../components/Spinner'
import { ROLES } from '../constants'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { userService } from '../services/userService'

const EMPTY_USER = { username: '', name: '', studyProgram: '', role: 'Student', password: '' }

// Same rules as UserCreateDto / UserUpdateDto in the API
function validate(form, creating) {
  const errors = {}
  const username = form.username.trim()
  if (username.length < 3 || username.length > 30) errors.username = 'Username must be 3-30 characters.'
  else if (!/^[A-Za-z0-9_.-]+$/.test(username))
    errors.username = 'Use only letters, numbers, dots, dashes and underscores.'

  if (!form.name.trim()) errors.name = 'Name is required.'
  else if (form.name.trim().length > 50) errors.name = 'Name can be at most 50 characters.'

  if (!form.studyProgram.trim()) errors.studyProgram = 'Program is required.'
  if (!ROLES.includes(form.role)) errors.role = 'Choose a role.'

  // Required when creating; optional when editing (empty = keep the current password)
  if ((creating || form.password) && (form.password.length < 8 || form.password.length > 100)) {
    errors[creating ? 'password' : 'newPassword'] = 'Password must be 8-100 characters.'
  }
  return errors
}

// /users/new and /users/:id/edit (admins only)
export default function UserFormPage() {
  const { id } = useParams()
  usePageTitle(id ? 'Edit user' : 'New user')

  const loadData = useCallback(
    () => Promise.all([id ? userService.get(id) : null, userService.getPrograms()]),
    [id],
  )
  const { data, loading, error, reload } = useApiData(loadData)

  if (loading && !data) return <Spinner />
  if (!data) return <ErrorMessage error={error} onRetry={error?.status === 404 ? undefined : reload} />

  const [user, programs] = data
  const initial = user
    ? { username: user.username, name: user.name, studyProgram: user.studyProgram, role: user.role, password: '' }
    : EMPTY_USER
  return <UserForm key={id ?? 'new'} userId={id} initial={initial} programs={programs} />
}

function UserForm({ userId, initial, programs }) {
  const navigate = useNavigate()
  const toast = useToast()
  const { user: me, refreshUser } = useAuth()
  const [form, setForm] = useState(initial)
  const [errors, setErrors] = useState({})
  const [error, setError] = useState(null)
  const [saving, setSaving] = useState(false)
  const creating = !userId
  const editingSelf = Number(userId) === me.id

  const update = (field) => (event) => {
    setForm({ ...form, [field]: event.target.value })
    setErrors({ ...errors, [field]: undefined, newPassword: undefined })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    const found = validate(form, creating)
    setErrors(found)
    setError(null)
    if (Object.keys(found).length > 0) return

    const { password, ...fields } = form
    setSaving(true)
    try {
      if (creating) {
        const created = await userService.create({ ...fields, password })
        toast.success(`${created.name} was created.`)
      } else {
        await userService.update(userId, { ...fields, newPassword: password || null })
        toast.success('User saved.')
        if (editingSelf) refreshUser()
      }
      navigate('/users')
    } catch (err) {
      setErrors(err.fieldErrors ?? {})
      setError(err)
      setSaving(false)
    }
  }

  const passwordError = errors.password ?? errors.newPassword

  return (
    <div style={{ maxWidth: 640, margin: '0 auto' }}>
      <Link className="back-link" to="/users">
        ← All users
      </Link>
      <div className="page-header">
        <h1>{creating ? 'New user' : `Edit ${initial.name}`}</h1>
      </div>

      <form className="card form" onSubmit={handleSubmit} noValidate>
        <ErrorMessage error={error} />
        <div className="form-row">
          <FormField label="Username" htmlFor="username" error={errors.username} hint="Used to sign in.">
            <input
              id="username"
              className={`input ${errors.username ? 'invalid' : ''}`}
              value={form.username}
              maxLength={30}
              autoComplete="off"
              onChange={update('username')}
            />
          </FormField>
          <FormField label="Name" htmlFor="name" error={errors.name}>
            <input
              id="name"
              className={`input ${errors.name ? 'invalid' : ''}`}
              value={form.name}
              maxLength={50}
              onChange={update('name')}
            />
          </FormField>
        </div>
        <div className="form-row">
          <FormField label="Study program" htmlFor="program" error={errors.studyProgram}>
            <input
              id="program"
              className={`input ${errors.studyProgram ? 'invalid' : ''}`}
              list="program-options"
              value={form.studyProgram}
              maxLength={100}
              onChange={update('studyProgram')}
            />
            <datalist id="program-options">
              {programs.map((p) => (
                <option key={p} value={p} />
              ))}
            </datalist>
          </FormField>
          <FormField
            label="Role"
            htmlFor="role"
            error={errors.role}
            hint={editingSelf ? "You can't remove your own admin role." : undefined}
          >
            <select
              id="role"
              className="select"
              value={form.role}
              onChange={update('role')}
              disabled={editingSelf}
            >
              {ROLES.map((r) => (
                <option key={r}>{r}</option>
              ))}
            </select>
          </FormField>
        </div>
        <FormField
          label={creating ? 'Password' : 'New password'}
          htmlFor="password"
          error={passwordError}
          hint={creating ? 'At least 8 characters.' : 'Leave empty to keep the current password.'}
        >
          <input
            id="password"
            type="password"
            className={`input ${passwordError ? 'invalid' : ''}`}
            value={form.password}
            autoComplete="new-password"
            onChange={update('password')}
          />
        </FormField>
        <div className="button-row">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Saving…' : creating ? 'Create user' : 'Save changes'}
          </button>
          <Link className="btn btn-ghost" to="/users">
            Cancel
          </Link>
        </div>
      </form>
    </div>
  )
}
