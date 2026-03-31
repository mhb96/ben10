import { Routes, Route } from 'react-router-dom'
import Home from './pages/Home.jsx'
import Quiz from './pages/Quiz.jsx'
import Loading from './pages/Loading.jsx'
import Result from './pages/Result.jsx'

export default function App() {
  return (
    <Routes>
      <Route path="/"           element={<Home />} />
      <Route path="/quiz"       element={<Quiz />} />
      <Route path="/loading"    element={<Loading />} />
      <Route path="/results/:id" element={<Result />} />
    </Routes>
  )
}
