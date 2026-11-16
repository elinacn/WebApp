import { useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import DifficultyBadge from '../components/DifficultyBadge'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import Spinner from '../components/Spinner'
import { useAuth } from '../context/authContext'
import { useApiData } from '../hooks/useApiData'
import { usePageTitle } from '../hooks/usePageTitle'
import { courseService } from '../services/courseService'

export default function CourseDetailPage() {
  const { id } = useParams()
  const { isAdmin } = useAuth()
  const loadCourse = useCallback(() => courseService.get(id), [id])
  const { data: course, loading, error, reload } = useApiData(loadCourse)
  usePageTitle(course?.code ?? 'Course')

  if (loading && !course) return <Spinner />

  return (
    <>
      <Link className="back-link" to="/courses">
        ← All courses
      </Link>

      {!course ? (
        <ErrorMessage error={error} onRetry={error?.status === 404 ? undefined : reload} />
      ) : (
        <>
          <div className="page-header">
            <div>
              <span className="badge">{course.code}</span>
              <h1>{course.title}</h1>
              {course.description && <p>{course.description}</p>}
            </div>
            <div className="button-row">
              <Link className="btn btn-primary" to={`/quests?courseId=${course.id}`}>
                Browse these quests
              </Link>
              {isAdmin && (
                <Link className="btn btn-secondary" to={`/courses/${course.id}/edit`}>
                  Edit course
                </Link>
              )}
            </div>
          </div>

          {course.quests.length === 0 ? (
            <EmptyState icon="🗺️" title="No quests in this course yet" />
          ) : (
            <section className="card">
              <div className="card-header">
                <h2>
                  {course.quests.length} {course.quests.length === 1 ? 'quest' : 'quests'}
                </h2>
              </div>
              <ul className="course-quest-list">
                {course.quests.map((quest) => (
                  <li key={quest.id}>
                    <span>{quest.title}</span>
                    <DifficultyBadge difficulty={quest.difficulty} />
                    <Link className="btn btn-secondary btn-sm" to={`/quests/${quest.id}/play`}>
                      Play
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}
        </>
      )}
    </>
  )
}
