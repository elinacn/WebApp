import { createContext, useContext } from 'react'

export const ToastContext = createContext(null)

// Short confirmation messages, e.g. toast.success('Course saved.') or toast.error('Could not delete.')
export function useToast() {
  return useContext(ToastContext)
}
