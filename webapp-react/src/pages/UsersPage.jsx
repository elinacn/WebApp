import { useCallback, useState } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import FormField from '../components/FormField'
import Spinner from '../components/Spinner'
import { ROLES } from '../constants'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { usePageTitle } from '../hooks/usePageTitle'
import { userService } from '../services/userService'

// User management (admins only)
export default function UsersPage() {
  usePageTitle('Users')
  const { user: me } = useAuth()
  const toast = useToast()
  const [search, setSearch] = useState('')
  const [program, setProgram] = useState('')
  const [role, setRole] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim())

  const loadUsers = useCallback(
    () => userService.getAll({ search: debouncedSearch, program, role }),
    [debouncedSearch, program, role],
  )
  const { data: users, loading, error, reload, setData: setUsers } = useApiData(loadUsers)

  const loadPrograms = useCallback(() => userService.getPrograms(), [])
  const { data: programs } = useApiData(loadPrograms)

  const [toDelete, setToDelete] = useState(null)
  const [deleting, setDeleting] = useState(false)

  const confirmDelete = async () => {
    setDeleting(true)
    try {
      await userService.remove(toDelete.id)
      setUsers((list) => list.filter((u) => u.id !== toDelete.id))
      toast.success(`${toDelete.name} was deleted.`)
      setToDelete(null)
    } catch (err) {
      toast.error(err.message)
    } finally {
      setDeleting(false)
    }
  }

  const hasFilters = Boolean(search || program || role)

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Users</h1>
          <p>Manage student and admin accounts.</p>
        </div>
        <Link className="btn btn-primary" to="/users/new">
          + New user
        </Link>
      </div>

      <div className="card filters" style={{ gridTemplateColumns: '2fr 1fr 1fr auto' }} role="search">
        <FormField label="Search" htmlFor="user-search">
          <input
            id="user-search"
            className="input"
            type="search"
            placeholder="Name or username…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </FormField>
        <FormField label="Program" htmlFor="user-program">
          <select id="user-program" className="select" value={program} onChange={(e) => setProgram(e.target.value)}>
            <option value="">All programs</option>
            {programs?.map((p) => (
              <option key={p}>{p}</option>
            ))}
          </select>
        </FormField>
        <FormField label="Role" htmlFor="user-role">
          <select id="user-role" className="select" value={role} onChange={(e) => setRole(e.target.value)}>
            <option value="">All roles</option>
            {ROLES.map((r) => (
              <option key={r}>{r}</option>
            ))}
          </select>
        </FormField>
        <button
          type="button"
          className="btn btn-ghost"
          onClick={() => {
            setSearch('')
            setProgram('')
            setRole('')
          }}
        >
          Reset
        </button>
      </div>

      <ErrorMessage error={error} onRetry={reload} />

      {loading && !users ? (
        <Spinner label="Loading users…" />
      ) : users?.length === 0 ? (
        <EmptyState icon="👤" title={hasFilters ? 'No users match your filters' : 'No users yet'} />
      ) : (
        users && (
          <div className="card table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Username</th>
                  <th>Program</th>
                  <th>Role</th>
                  <th>XP</th>
                  <th>Quests</th>
                  <th>
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <strong>{user.name}</strong>
                      {user.id === me.id && <span className="muted small"> (you)</span>}
                    </td>
                    <td className="muted">{user.username}</td>
                    <td>{user.studyProgram}</td>
                    <td>
                      <span className={`badge ${user.role === 'Admin' ? 'badge-admin' : ''}`}>{user.role}</span>
                    </td>
                    <td>{user.points}</td>
                    <td>{user.quizzesCompleted}</td>
                    <td className="actions">
                      <Link className="btn btn-secondary btn-sm" to={`/users/${user.id}/edit`}>
                        Edit
                      </Link>{' '}
                      {user.id !== me.id && (
                        <button
                          type="button"
                          className="btn btn-ghost btn-sm"
                          onClick={() => setToDelete(user)}
                          aria-label={`Delete ${user.name}`}
                        >
                          🗑
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete user?"
          message={`${toDelete.name} (${toDelete.username}) and all their quest results and streak days will be deleted. This can't be undone.`}
          busy={deleting}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}
    </>
  )
}
