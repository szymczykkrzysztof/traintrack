import { defineConfig, devices } from '@playwright/test'

const isCI = !!process.env.CI
const apiUrl = 'http://localhost:5087'
const webUrl = 'http://localhost:5173'

/**
 * Wymaga działającej bazy (`docker compose up -d db`). Playwright sam uruchamia API i Vite,
 * a lokalnie używa już działających instancji, jeśli są.
 * Connection string: lokalnie user-secrets (profil "http" = Development), w CI zmienna ConnectionStrings__Database.
 */
export default defineConfig({
  testDir: './tests',
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [['html', { open: 'never' }], ['github']] : [['list']],
  use: {
    baseURL: webUrl,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../../src/backend/TrainTrack.Api --launch-profile http',
      url: `${apiUrl}/health/live`,
      reuseExistingServer: !isCI,
      timeout: 120_000,
    },
    {
      command: 'npm --prefix ../../src/frontend run dev -- --port 5173 --strictPort',
      url: webUrl,
      reuseExistingServer: !isCI,
      env: { API_URL: apiUrl },
    },
  ],
})
