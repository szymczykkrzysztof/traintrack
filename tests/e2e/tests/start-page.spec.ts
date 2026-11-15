import { expect, test } from '@playwright/test'

// E1: pełny stos (PostgreSQL → API → proxy Vite → React).
test('start page shows that API and database work', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'TrainTrack' })).toBeVisible()
  const status = page.getByRole('list', { name: 'Status systemu' })
  await expect(status.getByText('API: działa')).toBeVisible()
  await expect(status.getByText('Baza danych: działa')).toBeVisible()
})
