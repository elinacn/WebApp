// The only place in the app that calls fetch. Every service file goes through here,
// so components never deal with URLs, headers, JSON or HTTP status codes.

// Thrown for every failed request, so pages can show `error.message` and
// forms can show `error.fieldErrors` next to the right input.
export class ApiError extends Error {
  constructor(status, message, fieldErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

// Called on 401 so the app can forget the signed-in user (set by AuthProvider)
let onUnauthorized = () => {}
export function setUnauthorizedHandler(handler) {
  onUnauthorized = handler
}

// ASP.NET names fields like "Questions[0].Options"; the React forms use
// "questions[0].options", so lower-case the first letter of each part.
function toFieldKey(key) {
  return key
    .split('.')
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join('.')
}

// Friendly messages for errors that don't come with one from the server
const defaultMessages = {
  0: 'Could not reach the server. Is the API running?',
  401: 'Please sign in to continue.',
  403: "You don't have permission to do that.",
  404: "We couldn't find what you were looking for.",
  500: 'Something went wrong on the server. Please try again.',
}

async function request(method, url, body) {
  let response
  try {
    response = await fetch(`/api${url}`, {
      method,
      credentials: 'same-origin',
      headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError(0, defaultMessages[0])
  }

  if (response.status === 204) return null

  const data = await response.json().catch(() => null)

  if (!response.ok) {
    if (response.status === 401 && url !== '/auth/login' && url !== '/auth/me') onUnauthorized()

    // ValidationProblemDetails: { errors: { Title: ["..."] } }
    const fieldErrors = Object.fromEntries(
      Object.entries(data?.errors ?? {}).map(([key, messages]) => [toFieldKey(key), messages[0]]),
    )
    const hasFieldErrors = Object.keys(fieldErrors).length > 0
    const message = hasFieldErrors
      ? 'Please fix the highlighted fields.'
      : data?.title ?? defaultMessages[response.status] ?? `Request failed (${response.status}).`
    throw new ApiError(response.status, message, fieldErrors)
  }

  return data
}

// Builds "?a=1&b=2" from an object, leaving out empty values
export function toQueryString(params = {}) {
  const query = new URLSearchParams(
    Object.entries(params).filter(([, value]) => value !== '' && value !== null && value !== undefined),
  ).toString()
  return query ? `?${query}` : ''
}

export const http = {
  get: (url) => request('GET', url),
  post: (url, body = {}) => request('POST', url, body),
  put: (url, body) => request('PUT', url, body),
  delete: (url) => request('DELETE', url),
}
