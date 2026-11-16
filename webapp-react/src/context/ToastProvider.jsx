import { useCallback, useMemo, useState } from 'react'
import { ToastContext } from './toastContext'

const TOAST_DURATION_MS = 3500

export default function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])

  const show = useCallback((message, type) => {
    const id = crypto.randomUUID()
    setToasts((current) => [...current, { id, message, type }])
    setTimeout(() => setToasts((current) => current.filter((t) => t.id !== id)), TOAST_DURATION_MS)
  }, [])

  const toast = useMemo(
    () => ({
      success: (message) => show(message, 'success'),
      error: (message) => show(message, 'error'),
    }),
    [show],
  )

  return (
    <ToastContext.Provider value={toast}>
      {children}
      <div className="toast-stack" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast ${t.type}`}>
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
