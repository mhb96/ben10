// ben10-frontend/src/components/ResultCard.jsx
export default function ResultCard({ character, description, imagePath, cardRef }) {
  return (
    <div ref={cardRef} style={{
      background: '#0a0a1a', border: '3px solid #00e5ff', borderRadius: 16,
      padding: '2rem', width: 360, display: 'flex', flexDirection: 'column',
      alignItems: 'center', gap: '1rem', color: '#f0f0f0'
    }}>
      <p style={{ color: '#00e5ff', fontSize: '0.85rem', fontWeight: 700 }}>
        BEN 10 PERSONALITY QUIZ
      </p>
      <img
        src={imagePath}
        alt={character}
        style={{ width: 180, height: 180, objectFit: 'contain' }}
        onError={e => { e.target.style.display = 'none' }}
      />
      <h2 style={{ color: '#00e5ff' }}>I got {character}!</h2>
      <p style={{ textAlign: 'center', fontSize: '0.9rem', color: '#ccc' }}>{description}</p>
    </div>
  )
}
