import { useEffect, useState, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchQuestions } from '../api/quizApi.js'
import ProgressBar from '../components/ProgressBar.jsx'

export default function Quiz() {
  const [questions, setQuestions] = useState([])
  const [current, setCurrent]     = useState(0)
  const [answers, setAnswers]     = useState({})
  const [error, setError]         = useState(null)
  const sessionId                 = useRef(crypto.randomUUID())
  const navigate                  = useNavigate()

  useEffect(() => {
    fetchQuestions()
      .then(setQuestions)
      .catch(() => setError('Could not load questions. Please try again.'))
  }, [])

  function selectAnswer(questionId, index) {
    const next = { ...answers, [questionId]: index }
    setAnswers(next)

    if (current + 1 < questions.length) {
      setCurrent(current + 1)
    } else {
      navigate('/loading', { state: { sessionId: sessionId.current, answers: next } })
    }
  }

  if (error) return <div className="page"><p className="error-text">{error}</p></div>
  if (!questions.length) return <div className="page"><p>Loading questions…</p></div>

  const q = questions[current]
  return (
    <div className="page">
      <ProgressBar current={current + 1} total={questions.length} />
      <p style={{ color: '#aaa' }}>Question {current + 1} of {questions.length}</p>
      <h2>{q.text}</h2>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', width: '100%' }}>
        {q.answers.map((a, i) => (
          <button key={i} className="btn secondary" onClick={() => selectAnswer(q.id, i)}>
            {a.text}
          </button>
        ))}
      </div>
    </div>
  )
}
