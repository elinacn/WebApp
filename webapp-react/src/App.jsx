import { BrowserRouter, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import ProtectedRoute from './components/ProtectedRoute'
import AuthProvider from './context/AuthProvider'
import ToastProvider from './context/ToastProvider'
import CourseDetailPage from './pages/CourseDetailPage'
import CourseFormPage from './pages/CourseFormPage'
import CoursesPage from './pages/CoursesPage'
import HomePage from './pages/HomePage'
import LoginPage from './pages/LoginPage'
import NotFoundPage from './pages/NotFoundPage'
import PlayQuestPage from './pages/PlayQuestPage'
import QuestFormPage from './pages/QuestFormPage'
import QuestsPage from './pages/QuestsPage'

export default function App() {
  return (
    <BrowserRouter>
      <ToastProvider>
        <AuthProvider>
          <Routes>
            <Route element={<Layout />}>
              <Route path="/" element={<HomePage />} />
              <Route path="/login" element={<LoginPage />} />
              <Route path="/quests" element={<QuestsPage />} />
              <Route
                path="/quests/new"
                element={
                  <ProtectedRoute adminOnly>
                    <QuestFormPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/quests/:id/edit"
                element={
                  <ProtectedRoute adminOnly>
                    <QuestFormPage />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/quests/:id/play"
                element={
                  <ProtectedRoute>
                    <PlayQuestPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/courses" element={<CoursesPage />} />
              <Route
                path="/courses/new"
                element={
                  <ProtectedRoute adminOnly>
                    <CourseFormPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/courses/:id" element={<CourseDetailPage />} />
              <Route
                path="/courses/:id/edit"
                element={
                  <ProtectedRoute adminOnly>
                    <CourseFormPage />
                  </ProtectedRoute>
                }
              />
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </AuthProvider>
      </ToastProvider>
    </BrowserRouter>
  )
}
