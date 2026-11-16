// Shows an error from the API (an ApiError or any Error), with an optional retry button
export default function ErrorMessage({ error, onRetry }) {
  if (!error) return null

  return (
    <div className="alert alert-error" role="alert">
      {error.message ?? String(error)}
      {onRetry && (
        <button type="button" className="btn btn-secondary btn-sm" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}
