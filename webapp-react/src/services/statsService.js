import { http, toQueryString } from './httpClient'

export const statsService = {
  // filters: { search, program, top } – all optional
  getLeaderboard: (filters) => http.get(`/leaderboard${toQueryString(filters)}`),
  // The signed-in user's XP, streak, week and recent quests
  getProfile: () => http.get('/profile'),
}
