import { http } from './httpClient'

export const authService = {
  login: (username, password) => http.post('/auth/login', { username, password }),
  logout: () => http.post('/auth/logout'),
  // The signed-in user, or an ApiError with status 401 when not signed in
  me: () => http.get('/auth/me'),
}
