import { useEffect, useState } from 'react'

// Returns `value` only after it has stopped changing for `delay` ms,
// so a search box doesn't send a request on every key press
export function useDebouncedValue(value, delay = 300) {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])

  return debounced
}
