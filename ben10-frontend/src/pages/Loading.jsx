import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { submitAnswers } from '../api/quizApi.js'

export default function Loading() {
  const { state }    = useLocation()
  const navigate     = useNavigate()
  const [error, setError] = useState(null)

  useEffect(() => {
    if (!state?.answers) { navigate('/'); return }

    submitAnswers(state.sessionId, state.answers)
      .then(result => navigate(`/results/${result.resultId}`, { state: { result } }))
      .catch(err   => setError(err.message))
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  if (error) return (
    <div className="page">
      <p className="error-text">{error}</p>
      <button className="btn" onClick={() => navigate('/quiz')}>Try Again</button>
    </div>
  )

  return (
    <div className="page">
      <p>Finding your alien match…</p>
    </div>
  )
}
