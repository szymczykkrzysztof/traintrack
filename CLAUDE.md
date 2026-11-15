# TrainTrack – instrukcje projektu

## Produkt
Aplikacja internetowa dla trenera personalnego i osoby trenującej: pakiety treningów
ze wspólnym licznikiem, historia sesji, karnety na siłownię, przypomnienia przed wygaśnięciem.
- Specyfikacja produktu: docs/product/
- Backlog i harmonogram: docs/development/backlog.md
- Aktualny stan prac: docs/development/status.md (czytaj na początku każdej sesji)

## Stos
- Backend: ASP.NET Core Web API (.NET LTS), modularny monolit
- Dane: PostgreSQL + EF Core (code-first, migracje)
- Auth: ASP.NET Core Identity + JWT + refresh token z rotacją
- Frontend: React + TypeScript + Vite + React Query
- Testy: xUnit, WebApplicationFactory + Testcontainers, Vitest, React Testing Library, Playwright
- Infrastruktura: Docker Compose, GitHub Actions

## Komendy
- docker compose up -d db
- dotnet build / dotnet test
- cd src/frontend && npm run lint && npm test
- cd tests/e2e && npx playwright test

## Reguły domenowe (żadna zmiana ich nie łamie)
- Pozostałe treningi wynikają z historii sesji i korekt, nie z pola licznika.
- Trener widzi tylko swoich klientów, klient tylko swoje dane.
- Karnet klienta jest prywatny, chyba że klient go udostępnił.
- Dane rozliczeniowe nie są usuwane, tylko zmieniają status.
- Czas zdarzeń w UTC, daty kalendarzowe jako DateOnly, strefa z konta użytkownika.

## Model pracy
Claude pisze cały kod produkcyjny i wszystkie testy. Użytkownik jest Product Ownerem
i recenzentem: akceptuje każdy krok po objaśnieniu. Użytkownik uczy się przez review,
więc objaśnienia są częścią zadania, a nie dodatkiem.

## Zasady pracy
- Najpierw plan, potem kod. Pracuj według docs/development/workflow.md.
- Jeden krok planu na raz, potem stop.
- Każdy krok kończ objaśnieniem wg docs/development/review-protocol.md
  i czekaj na „akceptuję” przed commitem.
- Testy przed implementacją; pokaż, że failują z właściwego powodu.
- Małe, skupione zmiany; bez refaktoryzacji niezwiązanego kodu.
- Każda zmiana schematu = migracja EF Core z objaśnieniem wygenerowanego SQL.
- Każda reguła biznesowa ma test; każdy endpoint ma test autoryzacji.
- Wyjaśniaj jak mentor mid .NET developera; nie upraszczaj ważnych szczegółów.
- Na koniec sesji zaktualizuj docs/development/status.md i docs/learning/journal.md.

## Quality gates
Build, testy unit + integration, lint i typecheck frontendu, E2E krytycznych ścieżek.

## Git
- Gałęzie: main (wydania), develop (integracja), feature/NN-nazwa (jeden slice).
- Conventional Commits: feat(packages): ..., test(auth): ..., docs: ...
- Nigdy nie commituj ani nie pushuj bez „akceptuję” od użytkownika.
