import { http, toQueryString } from './httpClient'

export const questService = {
  // filters: { search, difficulty, courseId, status, sort } – all optional
  search: (filters) => http.get(`/quests${toQueryString(filters)}`),
  getForPlay: (id) => http.get(`/quests/${id}/play`),
  // answers: [{ questionId, text, selected }]
  submit: (id, answers) => http.post(`/quests/${id}/submit`, { answers }),

  // Admin only
  getForEdit: (id) => http.get(`/quests/${id}`),
  create: (quest) => http.post('/quests', quest),
  update: (id, quest) => http.put(`/quests/${id}`, quest),
  remove: (id) => http.delete(`/quests/${id}`),
}
