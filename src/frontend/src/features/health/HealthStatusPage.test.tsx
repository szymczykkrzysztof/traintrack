import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { delay, http, HttpResponse } from 'msw'
import { describe, expect, it } from 'vitest'
import { renderWithQueryClient } from '../../test/renderWithQueryClient'
import { server } from '../../test/server'
import { HealthStatusPage } from './HealthStatusPage'

const READY = '*/health/ready'

const healthy = { status: 'Healthy', checks: { database: 'Healthy' } }
const databaseDown = { status: 'Unhealthy', checks: { database: 'Unhealthy' } }

describe('HealthStatusPage', () => {
  it('C1: shows a loading skeleton and no statuses while waiting for the API', async () => {
    server.use(http.get(READY, async () => {
      await delay('infinite')
      return HttpResponse.json(healthy)
    }))

    renderWithQueryClient(<HealthStatusPage />)

    expect(await screen.findByTestId('health-loading')).toHaveAttribute('aria-busy', 'true')
    expect(screen.queryByText(/API:/)).not.toBeInTheDocument()
    expect(screen.queryByText(/Baza danych:/)).not.toBeInTheDocument()
  })

  it('C2: shows that API and database work when readiness is healthy', async () => {
    server.use(http.get(READY, () => HttpResponse.json(healthy)))

    renderWithQueryClient(<HealthStatusPage />)

    expect(screen.getByRole('heading', { name: 'TrainTrack' })).toBeInTheDocument()
    expect(await screen.findByText('API: działa')).toBeInTheDocument()
    expect(screen.getByText('Baza danych: działa')).toBeInTheDocument()
  })

  it('C3: treats 503 with the health contract as data: API works, database is unavailable', async () => {
    server.use(http.get(READY, () => HttpResponse.json(databaseDown, { status: 503 })))

    renderWithQueryClient(<HealthStatusPage />)

    expect(await screen.findByText('API: działa')).toBeInTheDocument()
    expect(screen.getByText('Baza danych: niedostępna')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('C4: shows a connection error with a retry button when the network fails', async () => {
    server.use(http.get(READY, () => HttpResponse.error()))

    renderWithQueryClient(<HealthStatusPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Nie można połączyć się z API')
    expect(screen.getByRole('button', { name: 'Spróbuj ponownie' })).toBeInTheDocument()
  })

  it('C5: treats a response without the health contract (proxy 502) as API unavailable', async () => {
    server.use(http.get(READY, () => new HttpResponse('Bad Gateway', { status: 502 })))

    renderWithQueryClient(<HealthStatusPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('Nie można połączyć się z API')
    expect(screen.queryByText(/API: działa/)).not.toBeInTheDocument()
  })

  it('C6: sends a new request after clicking retry and shows the statuses', async () => {
    const user = userEvent.setup()
    let requests = 0
    server.use(http.get(READY, () => {
      requests += 1
      return requests === 1 ? HttpResponse.error() : HttpResponse.json(healthy)
    }))
    renderWithQueryClient(<HealthStatusPage />)

    await user.click(await screen.findByRole('button', { name: 'Spróbuj ponownie' }))

    expect(await screen.findByText('API: działa')).toBeInTheDocument()
    expect(screen.getByText('Baza danych: działa')).toBeInTheDocument()
    expect(requests).toBe(2)
  })

  it.each([
    ['healthy', healthy, 200],
    ['database down', databaseDown, 503],
  ])('C7: every status has a text label, not only a colour (%s)', async (_, body, status) => {
    server.use(http.get(READY, () => HttpResponse.json(body, { status })))

    renderWithQueryClient(<HealthStatusPage />)

    const list = await screen.findByRole('list', { name: 'Status systemu' })
    const items = within(list).getAllByRole('listitem')
    expect(items).toHaveLength(2)
    for (const item of items) {
      expect(item).toHaveTextContent(/^.+: (działa|niedostępna)$/)
    }
  })
})
