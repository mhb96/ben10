// ben10-frontend/src/api/quizApi.js
const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5252'

export async function fetchQuestions() {
  const res = await fetch(`${BASE}/api/quiz/questions`)
  if (!res.ok) throw new Error('Failed to load questions')
  return res.json()
}

export async function submitAnswers(sessionId, answers) {
  const res = await fetch(`${BASE}/api/quiz/submit`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, answers })
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({}))
    throw new Error(err.error ?? 'Submission failed')
  }
  return res.json()
}

export async function fetchResult(id) {
  const res = await fetch(`${BASE}/api/results/${id}`)
  if (!res.ok) throw new Error('Result not found')
  return res.json()
}
