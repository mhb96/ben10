import { useNavigate } from 'react-router-dom'

export default function Home() {
  const navigate = useNavigate()
  return (
    <div className="page">
      <h1>Which Ben 10 Alien Are You?</h1>
      <p style={{ textAlign: 'center', color: '#aaa' }}>
        Answer 10 questions and discover your inner alien from the original Ben 10 series.
      </p>
      <button className="btn" onClick={() => navigate('/quiz')}>Start Quiz</button>
    </div>
  )
}
