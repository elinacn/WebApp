import { CODE_BLANK, DIFFICULTIES } from '../constants'

export function emptyQuestion() {
  return {
    type: 'multipleChoice',
    text: '',
    points: 1,
    options: [
      { text: '', isCorrect: false },
      { text: '', isCorrect: false },
    ],
    acceptedAnswers: [''],
    caseSensitive: false,
    startCode: '',
    explanation: '',
  }
}

export function emptyQuest() {
  return { title: '', description: '', difficulty: '', courseId: '', questions: [emptyQuestion()] }
}

// The API's QuestFormDto -> form state. Every question gets all fields, so switching
// the type in the editor never loses what was typed.
export function toFormState(dto) {
  return {
    title: dto.title,
    description: dto.description ?? '',
    difficulty: dto.difficulty,
    courseId: dto.courseId ?? '',
    questions: dto.questions.map((q) => ({
      ...emptyQuestion(),
      ...q,
      options: q.options.length >= 2 ? q.options : emptyQuestion().options,
      acceptedAnswers: q.acceptedAnswers.length > 0 ? q.acceptedAnswers : [''],
      startCode: q.startCode ?? '',
      explanation: q.explanation ?? '',
    })),
  }
}

// Form state -> what the API expects
export function toRequest(form) {
  return {
    ...form,
    courseId: form.courseId === '' ? null : Number(form.courseId),
    questions: form.questions.map((q) => ({ ...q, points: q.points === '' ? 0 : q.points })),
  }
}

// Client-side version of the API's rules (data annotations + QuestValidator), so most
// mistakes are shown instantly. The API checks everything again.
export function validateQuest(form) {
  const errors = {}
  const title = form.title.trim()

  if (title.length < 2 || title.length > 100) errors.title = 'Title must be 2-100 characters.'
  if (form.description.length > 500) errors.description = 'Description can be at most 500 characters.'
  if (!DIFFICULTIES.includes(form.difficulty)) errors.difficulty = 'Choose a difficulty.'
  if (form.questions.length === 0) errors.questions = 'Add at least one question.'

  form.questions.forEach((q, i) => {
    const key = (field) => `questions[${i}].${field}`

    if (!q.text.trim()) errors[key('text')] = 'Question text is required.'
    if (q.points === '' || q.points < 0 || q.points > 1000) errors[key('points')] = 'Points must be between 0 and 1000.'

    if (q.type === 'multipleChoice') {
      const filled = q.options.filter((o) => o.text.trim())
      if (filled.length < 2) errors[key('options')] = 'Fill in at least two options.'
      else if (!filled.some((o) => o.isCorrect)) errors[key('options')] = 'Mark at least one option as correct.'
      else if (q.options.some((o) => o.isCorrect && !o.text.trim()))
        errors[key('options')] = "A correct option can't be empty."
    } else {
      if (!q.acceptedAnswers.some((a) => a.trim())) errors[key('acceptedAnswers')] = 'Add at least one accepted answer.'
      if (q.type === 'code' && q.startCode.split(CODE_BLANK).length - 1 !== 1)
        errors[key('startCode')] = `The start code must contain exactly one ${CODE_BLANK} blank.`
    }
  })

  return errors
}
