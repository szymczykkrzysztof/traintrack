# Slice 0 – Walking skeleton

- **Status:** Accepted (2026-11-15)
- **Gałąź:** feature/00-walking-skeleton
- **Podstawa:** ADR-001 (PostgreSQL), ADR-002 (struktura projektów), [api.md](../../docs/architecture/api.md) (`/health/live`, `/health/ready`), [overview.md](../../docs/architecture/overview.md) (stos)

## User story
Jako developer chcę mieć działający szkielet całego systemu (frontend → API → baza → CI),
żeby każdy kolejny slice dodawał funkcję do sprawdzonej, zintegrowanej całości.

## Zakres

**Backend** (`src/backend/`, .NET 10 LTS przypięty w `global.json`)
- Projekty wg ADR-002: `TrainTrack.Domain`, `TrainTrack.Application`, `TrainTrack.Infrastructure`, `TrainTrack.Api`, z referencjami tylko w kierunku Api → Application → Domain oraz Infrastructure → Application → Domain. `Domain` i `Application` są puste (bez encji).
- EF Core + Npgsql: `AppDbContext` w `Infrastructure` i pierwsza, pusta migracja.
- Migracje:
  - lokalnie stosuje je developer (`dotnet ef database update`);
  - testy integracyjne stosują je w fixturze przed testami;
  - API nie migruje bazy przy starcie.
- Health checks (ASP.NET Core Health Checks):
  - `GET /health/live`: proces API działa; nie sprawdza bazy;
  - `GET /health/ready`: API i PostgreSQL; zwraca JSON ze stanem ogólnym i stanem każdego sprawdzenia (`database`), bez szczegółów technicznych.

**Frontend** (`src/frontend/`)
- Vite + React + TypeScript + TanStack Query.
- Strona startowa pokazuje „TrainTrack” i status API oraz bazy z `GET /health/ready`. Teksty po polsku.
- Proxy Vite przekazuje `/api` i `/health` do API, więc frontend i API mają jedno źródło (origin) i CORS nie jest włączany.

**Testy** (`tests/`, wg ADR-002)
- `TrainTrack.Domain.Tests` i `TrainTrack.Application.Tests`: projekty z jednym testem dymnym (smoke test), żeby pipeline i konwencje istniały od początku.
- `TrainTrack.Architecture.Tests`: kierunek zależności między projektami (ADR-002).
- `TrainTrack.Api.IntegrationTests`: `WebApplicationFactory` + Testcontainers (PostgreSQL).
- Vitest + React Testing Library: komponent strony startowej.
- `tests/e2e`: Playwright, jeden test strony startowej.

**Infrastruktura**
- `docker-compose.yml`: usługa `db` (PostgreSQL z przypiętą wersją główną obrazu, wolumen na dane). API i frontend uruchamiane z terminala lub IDE (overview.md, „Środowisko lokalne i CI”).
- Ta sama przypięta wersja PostgreSQL w Testcontainers.
- GitHub Actions:

  | Job | Kroki |
  | --- | --- |
  | `backend` | restore → build → testy (unit, architektura, integracyjne) |
  | `frontend` | `npm ci` → lint → typecheck → Vitest → build |
  | `e2e` | baza + API + frontend → Playwright → raport HTML jako artefakt |

## Reguły biznesowe
- Brak (slice techniczny).

## Kryteria akceptacji (Given / When / Then)
1. Given uruchomiona baza (`docker compose up -d db`) When wywołam `GET /health/ready`
   Then dostaję 200, stan ogólny `Healthy` i `database: Healthy`.
2. Given zatrzymana baza When wywołam `GET /health/ready`
   Then dostaję 503, stan ogólny `Unhealthy` i `database: Unhealthy`.
3. Given zatrzymana baza When wywołam `GET /health/live` Then dostaję 200 (API żyje, choć nie jest gotowe).
4. Given uruchomione baza, API i frontend When otworzę stronę startową
   Then widzę „TrainTrack” oraz status API i bazy.
5. Given API niedostępne When otworzę stronę startową
   Then widzę czytelny komunikat błędu z przyciskiem „Spróbuj ponownie”, a nie pustą stronę.
6. Given repozytorium bez zmian When uruchomię komendy z CLAUDE.md
   (`dotnet build`, `dotnet test`, `npm run lint && npm test`, `npx playwright test`) Then wszystkie przechodzą.
7. Given PR do `develop` When pipeline CI się wykona Then joby `backend`, `frontend` i `e2e` są zielone.

## Scenariusze negatywne i brzegowe
- Brak connection stringa → API nie startuje i loguje czytelny błąd konfiguracji (walidacja opcji przy starcie).
- Wolna odpowiedź bazy → sprawdzenie `database` kończy się po limicie czasu i zwraca `Unhealthy`, zamiast wisieć. Limit: 3 s.
- Podczas ładowania strona startowa pokazuje szkielet (skeleton), a nie pustą stronę (screens.md, „Stany”).

## Bezpieczeństwo i autoryzacja
- `/health/live` i `/health/ready` są publiczne (`[AllowAnonymous]`), ale nie ujawniają connection stringa, wersji, nazwy hosta, komunikatów wyjątków ani stack trace.
- Sekrety tylko w `user-secrets` i zmiennych środowiskowych, nigdy w repozytorium. `docker-compose.yml` używa haseł wyłącznie deweloperskich, oznaczonych jako takie.
- Brak CORS: jedno źródło przez proxy Vite.

## Poza zakresem
- Uwierzytelnianie (slice 1–2, ADR-003), encje domenowe, seeder danych demonstracyjnych.
- OpenAPI i generowany klient TypeScript (SYS-07, etap 4).
- Serilog i logi strukturalne (SYS-08), Mailpit (etap 6).
- Konteneryzacja API i frontendu oraz wdrożenie na serwer.

## Testy

| Poziom | Scenariusz | Oczekiwany wynik |
| --- | --- | --- |
| Unit | Test dymny w `Domain.Tests` i `Application.Tests` | Zielony; potwierdza konfigurację projektów |
| Architektura | `Domain` nie zależy od innych projektów ani od EF Core; `Application` nie zależy od `Infrastructure` ani `Api` | Zielony; upada po dodaniu zakazanej referencji |
| Integration | `/health/ready` z działającym PostgreSQL (Testcontainers) | 200, `Healthy`, `database: Healthy` |
| Integration | `/health/ready` z niedostępną bazą | 503, `Unhealthy`, treść bez szczegółów technicznych |
| Integration | `/health/live` z niedostępną bazą | 200 |
| Integration | Start API bez connection stringa | Start kończy się błędem walidacji konfiguracji |
| Component (RTL) | Strona startowa: ładowanie, sukces, błąd (API niedostępne), „Spróbuj ponownie” | Właściwy stan widoczny; ponowienie wysyła nowe zapytanie |
| E2E | Strona startowa z działającym stosem | Widoczne „TrainTrack” i status `Healthy` dla API i bazy |

## Decyzje PO (2026-11-15)
1. Limit czasu sprawdzenia bazy: 3 s.
2. Job `e2e` w CI od slice'a 0.

## Plan implementacji (zaakceptowany 2026-11-15)

| # | Krok | Gotowe, gdy |
| --- | --- | --- |
| 1 | Szkielet rozwiązania .NET: `global.json`, `TrainTrack.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, projekty `src/backend/*`, testy dymne | `dotnet build` i `dotnet test` przechodzą |
| 2 | Testy architektury (ADR-002) z testem mutacyjnym | Zielone; upadają na zakazanej referencji |
| 3 | `docker-compose.yml` (`db`), `AppDbContext`, pusta migracja, walidacja `DatabaseOptions`, fixture Testcontainers | Test „start bez connection stringa” zielony |
| – | Tabela przypadków testowych (krok 3 workflow) do akceptacji PO | Akceptacja |
| 4 | `/health/live`, `/health/ready`, limit 3 s, JSON bez szczegółów | Kryteria 1–3 |
| 5 | Frontend: Vite + React + TS + TanStack Query, proxy, strona startowa | Lint + Vitest zielone; kryteria 4–5 |
| 6 | Playwright E2E | Kryterium 6 |
| 7 | GitHub Actions: `backend`, `frontend`, `e2e` | Kryterium 7 (push/PR za zgodą PO) |

Decyzje techniczne PO: Shouldly (asercje), NetArchTest.eNhancedEdition (testy architektury), xUnit v3, nazwy w bazie `snake_case` (EFCore.NamingConventions), Node 24 LTS w `.nvmrc`.
