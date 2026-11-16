import { useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { usePageTitle } from '../hooks/usePageTitle'

// Client-side check before calling the API; the API validates again
function validate({ username, password }) {
  const errors = {}
  if (!username.trim()) errors.username = 'Enter your username.'
  if (!password) errors.password = 'Enter your password.'
  return errors
}

export default function LoginPage() {
  usePageTitle('Sign in')
  const { user, login } = useAuth()
  const toast = useToast()
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ username: '', password: '' })
  const [errors, setErrors] = useState({})
  const [error, setError] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  // Where to go after signing in: the page that asked for it, or home
  const returnTo = location.state?.from?.pathname ?? '/'

  if (user && !submitting) return <Navigate to={returnTo} replace />

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

    setSubmitting(true)
    try {
      const signedIn = await login(form.username, form.password)
      toast.success(`Welcome back, ${signedIn.name}!`)
      navigate(returnTo, { replace: true })
    } catch (err) {
      setErrors(err.fieldErrors ?? {})
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <div className="card login-card">
        <h1>Sign in</h1>
        <p className="muted" style={{ textAlign: 'center' }}>
          Sign in to play quests, earn XP and keep your streak.
        </p>

        <ErrorMessage error={error} />

        <form className="form" onSubmit={handleSubmit} noValidate>
          <FormField label="Username" htmlFor="username" error={errors.username}>
            <input
              id="username"
              className={`input ${errors.username ? 'invalid' : ''}`}
              value={form.username}
              onChange={update('username')}
              autoComplete="username"
              autoFocus
            />
          </FormField>
          <FormField label="Password" htmlFor="password" error={errors.password}>
            <input
              id="password"
              type="password"
              className={`input ${errors.password ? 'invalid' : ''}`}
              value={form.password}
              onChange={update('password')}
              autoComplete="current-password"
            />
          </FormField>
          <button type="submit" className="btn btn-primary btn-block" disabled={submitting}>
            {submitting ? 'Signing in…' : 'Sign in'}
          </button>
        </form>

        {import.meta.env.DEV && (
          <p className="demo-hint muted">
            Demo accounts: <code>admin</code> (admin) or <code>anna</code> (student), password <code>quest1234</code>
          </p>
        )}
      </div>
    </div>
  )
}
