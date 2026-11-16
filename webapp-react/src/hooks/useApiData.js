import { useCallback, useEffect, useState } from 'react'

// Loads data from the API and tracks loading/error state.
// `loader` must be stable (wrap it in useCallback); a new loader triggers a new request.
// While a new request runs, the previous data stays visible so lists don't flicker.
export function useApiData(loader) {
  const [version, setVersion] = useState(0)
  const [result, setResult] = useState({ loader: null, version: -1, data: null, error: null })

  useEffect(() => {
    let active = true
    loader().then(
      (data) => active && setResult({ loader, version, data, error: null }),
      (error) => active && setResult((r) => ({ loader, version, data: r.data, error })),
    )
    // Ignore the answer if the component unmounted or a newer request started
    return () => {
      active = false
    }
  }, [loader, version])

  const loading = result.loader !== loader || result.version !== version
  const reload = useCallback(() => setVersion((v) => v + 1), [])
  const setData = useCallback(
    (update) => setResult((r) => ({ ...r, data: typeof update === 'function' ? update(r.data) : update })),
    [],
  )

  return { data: result.data, error: loading ? null : result.error, loading, reload, setData }
}
