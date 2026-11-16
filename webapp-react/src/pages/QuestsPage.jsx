import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import ConfirmDialog from '../components/ConfirmDialog'
import EmptyState from '../components/EmptyState'
import ErrorMessage from '../components/ErrorMessage'
import QuestCard from '../components/QuestCard'
import QuestFilters from '../components/QuestFilters'
import Spinner from '../components/Spinner'
import { useAuth } from '../context/authContext'
import { useToast } from '../context/toastContext'
import { useApiData } from '../hooks/useApiData'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { usePageTitle } from '../hooks/usePageTitle'
import { courseService } from '../services/courseService'
import { questService } from '../services/questService'
import './QuestsPage.css'

export default function QuestsPage() {
  usePageTitle('Quests')
  const { user, isAdmin } = useAuth()
  const toast = useToast()

  // Filters live in the URL (?difficulty=Beginner&sort=xp), so they survive a refresh,
  // work with the back button and can be shared as a link
  const [params, setParams] = useSearchParams()
  const search = params.get('search') ?? ''
  const difficulty = params.get('difficulty') ?? ''
  const courseId = params.get('courseId') ?? ''
  const status = params.get('status') ?? ''
  const sort = params.get('sort') || 'newest'

  // The search box updates instantly; the URL (and the API call) waits until typing stops
  const [searchText, setSearchText] = useState(search)
  const debouncedSearch = useDebouncedValue(searchText)

  const updateFilter = useCallback(
    (name, value) =>
      setParams(
        (current) => {
          const next = new URLSearchParams(current)
          if (value) next.set(name, value)
          else next.delete(name)
          return next
        },
        { replace: true },
      ),
    [setParams],
  )

  useEffect(() => {
    updateFilter('search', debouncedSearch.trim())
  }, [debouncedSearch, updateFilter])

  const handleFilterChange = (name, value) => (name === 'search' ? setSearchText(value) : updateFilter(name, value))

  const handleReset = () => {
    setSearchText('')
    setParams({}, { replace: true })
  }

  // Reload the list whenever a filter (or the signed-in user) changes
  const userId = user?.id
  const loadQuests = useCallback(
    () => questService.search({ search, difficulty, courseId, status: userId ? status : '', sort }),
    [search, difficulty, courseId, status, sort, userId],
  )
  const { data: quests, loading, error, reload, setData: setQuests } = useApiData(loadQuests)

  const loadCourses = useCallback(() => courseService.getAll(), [])
  const { data: courses } = useApiData(loadCourses)

  // Delete flow: pick a quest → confirm in the dialog → remove it from the list
  const [toDelete, setToDelete] = useState(null)
  const [deleting, setDeleting] = useState(false)

  const confirmDelete = async () => {
    setDeleting(true)
    try {
      await questService.remove(toDelete.id)
      setQuests((list) => list.filter((q) => q.id !== toDelete.id))
      toast.success(`"${toDelete.title}" was deleted.`)
      setToDelete(null)
    } catch (err) {
      toast.error(err.message)
    } finally {
      setDeleting(false)
    }
  }

  const hasFilters = Boolean(search || difficulty || courseId || status)

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Quests</h1>
          <p>Pick a quest, answer the questions and earn XP the first time you complete it.</p>
        </div>
        {isAdmin && (
          <Link className="btn btn-primary" to="/quests/new">
            + New quest
          </Link>
        )}
      </div>

      <QuestFilters
        filters={{ search: searchText, difficulty, courseId, status, sort }}
        courses={courses ?? []}
        signedIn={Boolean(user)}
        onChange={handleFilterChange}
        onReset={handleReset}
      />

      <ErrorMessage error={error} onRetry={reload} />

      {loading && !quests ? (
        <Spinner label="Loading quests…" />
      ) : quests?.length === 0 ? (
        <EmptyState title={hasFilters ? 'No quests match your filters' : 'No quests yet'}>
          {hasFilters ? (
            <button type="button" className="btn btn-secondary" onClick={handleReset}>
              Clear filters
            </button>
          ) : (
            isAdmin && (
              <Link className="btn btn-primary" to="/quests/new">
                Create the first quest
              </Link>
            )
          )}
        </EmptyState>
      ) : (
        quests && (
          <>
            <p className="result-count" aria-live="polite">
              {quests.length} {quests.length === 1 ? 'quest' : 'quests'}
              {loading && ' · updating…'}
            </p>
            <div className="grid">
              {quests.map((quest) => (
                <QuestCard
                  key={quest.id}
                  quest={quest}
                  signedIn={Boolean(user)}
                  isAdmin={isAdmin}
                  onDelete={setToDelete}
                />
              ))}
            </div>
          </>
        )
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete quest?"
          message={`"${toDelete.title}" and all its questions will be deleted, along with everyone's results for it. This can't be undone.`}
          busy={deleting}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}
    </>
  )
}
