import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Calls to /api go to the ASP.NET API (WebApp.Api). The browser only ever talks to
    // this dev server, so the API's login cookie is same-origin and needs no CORS setup.
    proxy: {
      '/api': {
        target: 'http://localhost:5114',
        changeOrigin: true,
      },
    },
  },
})
