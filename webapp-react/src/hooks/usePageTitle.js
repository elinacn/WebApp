import { useEffect } from 'react'

// Sets the browser tab title, e.g. "Quests · CodeQuest"
export function usePageTitle(title) {
  useEffect(() => {
    document.title = title ? `${title} · CodeQuest` : 'CodeQuest'
  }, [title])
}
