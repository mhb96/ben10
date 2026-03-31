export default function ProgressBar({ current, total }) {
  const pct = Math.round((current / total) * 100)
  return (
    <div style={{ width: '100%', background: '#1a1a2e', borderRadius: 8, height: 10 }}>
      <div style={{
        width: `${pct}%`, background: '#00e5ff',
        height: '100%', borderRadius: 8, transition: 'width 0.3s'
      }} />
    </div>
  )
}
