export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy'

/** Kontrakt /health/ready (spec slice'a 0): wyłącznie statusy, bez szczegółów technicznych. */
export interface HealthReport {
  status: HealthStatus
  checks: Record<string, HealthStatus>
}

const statuses: readonly string[] = ['Healthy', 'Degraded', 'Unhealthy']

function isHealthStatus(value: unknown): value is HealthStatus {
  return typeof value === 'string' && statuses.includes(value)
}

function isHealthReport(value: unknown): value is HealthReport {
  if (typeof value !== 'object' || value === null) return false
  const { status, checks } = value as Record<string, unknown>
  return isHealthStatus(status)
    && typeof checks === 'object' && checks !== null
    && Object.values(checks).every(isHealthStatus)
}

export class ApiUnavailableError extends Error {
  constructor(options?: ErrorOptions) {
    super('API is unavailable', options)
    this.name = 'ApiUnavailableError'
  }
}

/**
 * 200 i 503 z poprawnym kontraktem to dane: API odpowiedziało, a 503 oznacza tylko niedostępną zależność.
 * Błąd sieci albo odpowiedź bez kontraktu (np. 502 z proxy) oznacza, że API jest niedostępne.
 */
export async function fetchReadiness(signal?: AbortSignal): Promise<HealthReport> {
  let response: Response
  try {
    response = await fetch(new URL('/health/ready', window.location.origin), { signal })
  } catch (error) {
    throw new ApiUnavailableError({ cause: error })
  }

  if (response.status !== 200 && response.status !== 503) {
    throw new ApiUnavailableError()
  }

  const body: unknown = await response.json().catch(() => undefined)
  if (!isHealthReport(body)) {
    throw new ApiUnavailableError()
  }

  return body
}
