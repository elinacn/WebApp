import { http, toQueryString } from './httpClient'

// Admin only
export const userService = {
  // filters: { search, program, role } – all optional
  getAll: (filters) => http.get(`/users${toQueryString(filters)}`),
  getPrograms: () => http.get('/users/programs'),
  get: (id) => http.get(`/users/${id}`),
  create: (user) => http.post('/users', user),
  update: (id, user) => http.put(`/users/${id}`, user),
  remove: (id) => http.delete(`/users/${id}`),
}
