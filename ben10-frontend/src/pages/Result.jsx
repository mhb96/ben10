// ben10-frontend/src/pages/Result.jsx
import { useEffect, useRef, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import html2canvas from 'html2canvas'
import { fetchResult } from '../api/quizApi.js'
import ResultCard from '../components/ResultCard.jsx'

export default function Result() {
  const { id }       = useParams()
  const { state }    = useLocation()
  const navigate     = useNavigate()
  const cardRef      = useRef(null)
  const [result, setResult] = useState(state?.result ?? null)
  const [error, setError]   = useState(null)

  useEffect(() => {
    if (!result) {
      fetchResult(id)
        .then(data => setResult({
          resultId: data.id,
          matchedCharacter: data.matchedCharacter,
          description: '',
          imagePath: `/images/aliens/${data.matchedCharacter.toLowerCase().replace(/\s+/g, '')}.png`,
          matchedTraits: []
        }))
        .catch(() => setError('Could not load result.'))
    }
  }, [id]) // eslint-disable-line react-hooks/exhaustive-deps

  async function downloadCard() {
    if (!cardRef.current) return
    const canvas = await html2canvas(cardRef.current, { backgroundColor: null })
    const link = document.createElement('a')
    link.download = `ben10-${result.matchedCharacter.toLowerCase()}.png`
    link.href = canvas.toDataURL('image/png')
    link.click()
  }

  if (error)  return <div className="page"><p className="error-text">{error}</p></div>
  if (!result) return <div className="page"><p>Loading…</p></div>

  return (
    <div className="page">
      <h1>You are {result.matchedCharacter}!</h1>
      {result.matchedTraits?.length > 0 && (
        <p style={{ color: '#aaa' }}>
          Your top traits: <strong>{result.matchedTraits.join(', ')}</strong>
        </p>
      )}
      <ResultCard
        character={result.matchedCharacter}
        description={result.description}
        imagePath={result.imagePath}
        cardRef={cardRef}
      />
      <button className="btn" onClick={downloadCard}>Download Card</button>
      <button className="btn secondary" onClick={() => navigate('/quiz')}>Retake Quiz</button>
      <p style={{ color: '#aaa', fontSize: '0.8rem' }}>
        Share link: {window.location.href}
      </p>
    </div>
  )
}
