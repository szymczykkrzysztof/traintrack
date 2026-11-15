import { setupServer } from 'msw/node'

/** Serwer MSW dla testów komponentów; każdy test ustawia własne odpowiedzi przez server.use(). */
export const server = setupServer()
