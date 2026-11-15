import { useQuery } from '@tanstack/react-query'
import { fetchReadiness, type HealthStatus } from './api'
import './HealthStatusPage.css'

const labels: Record<HealthStatus, string> = {
  Healthy: 'działa',
  Degraded: 'działa z problemami',
  Unhealthy: 'niedostępna',
}

export function HealthStatusPage() {
  const readiness = useQuery({
    queryKey: ['health', 'ready'],
    queryFn: ({ signal }) => fetchReadiness(signal),
    retry: false,
  })

  return (
    <main className="health-page">
      <h1>TrainTrack</h1>

      {readiness.isPending && (
        <div className="health-skeleton" data-testid="health-loading" aria-busy="true" aria-label="Ładowanie statusu">
          <span />
          <span />
        </div>
      )}

      {readiness.isError && (
        <div className="health-error">
          <p role="alert">Nie można połączyć się z API</p>
          <button type="button" onClick={() => readiness.refetch()}>Spróbuj ponownie</button>
        </div>
      )}

      {readiness.isSuccess && (
        <ul className="health-list" aria-label="Status systemu">
          {/* Skoro przyszła odpowiedź z kontraktem, API działa; o bazie mówi sprawdzenie "database". */}
          <li data-status="Healthy">API: {labels.Healthy}</li>
          <li data-status={readiness.data.checks.database ?? 'Unhealthy'}>
            Baza danych: {labels[readiness.data.checks.database ?? 'Unhealthy']}
          </li>
        </ul>
      )}
    </main>
  )
}
