// Values that must match the API (WebApp.Api/Models)

export const DIFFICULTIES = ['Beginner', 'Intermediate', 'Expert', 'Master']

// XP awarded the first time a quest of each difficulty is completed (DifficultyRules in the API)
export const DIFFICULTY_XP = { Beginner: 10, Intermediate: 25, Expert: 40, Master: 60 }

export const QUESTION_TYPES = [
  { value: 'multipleChoice', label: 'Multiple choice' },
  { value: 'freeText', label: 'Free text' },
  { value: 'code', label: 'Code (fill in the blank)' },
]

export const ROLES = ['Student', 'Admin']

export const CODE_BLANK = '____'
