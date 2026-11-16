import { useCallback, useState } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import Spinner from '../components/Spinner'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { usePageTitle } from '../hooks/usePageTitle'
import { courseService } from '../services/courseService'

export default function CoursesPage() {
  usePageTitle('Courses')
  const { isAdmin } = useAuth()
  const toast = useToast()
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebouncedValue(search.trim())

  const loadCourses = useCallback(() => courseService.getAll(debouncedSearch), [debouncedSearch])
  const { data: courses, loading, error, reload, setData: setCourses } = useApiData(loadCourses)

  const [toDelete, setToDelete] = useState(null)
  const [deleting, setDeleting] = useState(false)

  const confirmDelete = async () => {
    setDeleting(true)
    try {
      await courseService.remove(toDelete.id)
      setCourses((list) => list.filter((c) => c.id !== toDelete.id))
      toast.success(`${toDelete.code} was deleted.`)
      setToDelete(null)
    } catch (err) {
      toast.error(err.message)
    } finally {
      setDeleting(false)
    }
  }

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Courses</h1>
          <p>Quests are grouped into courses. Open a course to see its quests.</p>
        </div>
        {isAdmin && (
          <Link className="btn btn-primary" to="/courses/new">
            + New course
          </Link>
        )}
      </div>

      <div className="card filters" style={{ gridTemplateColumns: '1fr' }} role="search">
        <input
          className="input"
          type="search"
          placeholder="Search by code or title…"
          aria-label="Search courses"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <ErrorMessage error={error} onRetry={reload} />

      {loading && !courses ? (
        <Spinner label="Loading courses…" />
      ) : courses?.length === 0 ? (
        <EmptyState icon="📚" title={search ? 'No courses match your search' : 'No courses yet'} />
      ) : (
        <div className="grid">
          {courses?.map((course) => (
            <article key={course.id} className="card quest-card">
              <span className="badge">{course.code}</span>
              <h3>{course.title}</h3>
              {course.description && <p className="muted">{course.description}</p>}
              <p className="muted small">
                📝 {course.questCount} {course.questCount === 1 ? 'quest' : 'quests'}
              </p>
              <div className="button-row" style={{ marginTop: 'auto' }}>
                <Link className="btn btn-primary" to={`/courses/${course.id}`}>
                  View course
                </Link>
                {isAdmin && (
                  <>
                    <Link className="btn btn-secondary" to={`/courses/${course.id}/edit`}>
                      Edit
                    </Link>
                    <button
                      type="button"
                      className="btn btn-ghost"
                      onClick={() => setToDelete(course)}
                      aria-label={`Delete ${course.code}`}
                    >
                      🗑
                    </button>
                  </>
                )}
              </div>
            </article>
          ))}
        </div>
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete course?"
          message={`${toDelete.code} – ${toDelete.title} will be deleted. Its ${toDelete.questCount} quests are kept, just without a course.`}
          busy={deleting}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}
    </>
  )
}
