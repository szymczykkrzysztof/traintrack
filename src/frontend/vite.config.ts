/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Adres API dla proxy w trybie deweloperskim; frontend i API mają jedno źródło, więc CORS nie jest potrzebny.
const apiUrl = process.env.API_URL ?? 'http://localhost:5087'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': apiUrl,
      '/health': apiUrl,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    restoreMocks: true,
  },
})
