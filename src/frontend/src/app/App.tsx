import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { HealthStatusPage } from '../features/health/HealthStatusPage'

const queryClient = new QueryClient()

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <HealthStatusPage />
    </QueryClientProvider>
  )
}
