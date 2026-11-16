import { http, toQueryString } from './httpClient'

export const courseService = {
  getAll: (search) => http.get(`/courses${toQueryString({ search })}`),
  get: (id) => http.get(`/courses/${id}`),

  // Admin only
  create: (course) => http.post('/courses', course),
  update: (id, course) => http.put(`/courses/${id}`, course),
  remove: (id) => http.delete(`/courses/${id}`),
}
