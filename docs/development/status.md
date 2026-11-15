# Status prac

> Claude czyta ten plik na początku każdej sesji i aktualizuje go na końcu (skill `close-session`).

## Aktualny etap
Slice 0 (walking skeleton) zaimplementowany na gałęzi feature/00-walking-skeleton; czeka na push, PR do develop i zielony CI.

## Zrobione
- [x] Struktura repozytorium, CLAUDE.md, protokół akceptacji, workflow, backlog, strategia testów.
- [x] Podział docs/product/spec-traintrack.md na pliki w docs/product/ i docs/architecture/ (2026-11-15).
- [x] Sesja analityka: 33 decyzje BA-01…BA-33 w docs/product/business-rules.md (2026-11-15).

## Do zrobienia teraz
- [x] Wprowadzić punkty „Do uzupełnienia” z BA-01…BA-33 do dokumentów (2026-11-15):
  - [x] database.md: `Payment.Status`, `Package.CancellationWindowHours`, status `GymMembership`, tabela udostępnień karnetu, nakładanie sesji klienta;
  - [x] api.md: wpłata początkowa w `POST /api/packages`, anulowanie wpłaty, `reason` przy anulowaniu pakietu, potwierdzenie i odrzucenie prośby, lista trenerów w `sharing`;
  - [x] notifications.md: próg z pozostałych sesji, strefa trenera, nowe powiadomienia (późne odwołanie, anulowanie pakietu, rozstrzygnięcie sporu, nowa prośba);
  - [x] screens.md: T-01 (spory, do rozliczenia), T-02 (Ewa, filtr Zarchiwizowani), T-04 (metoda płatności), K-03 (wybór trenerów);
  - [x] business-rules.md: statusy `Requested`, `Rejected`, `Withdrawn` w „Statusy sesji”.
- [x] Backlog: nowa funkcja TRN-16 (P1, etap 7, slice 8) – przepięcie sesji (BA-03).
- [x] ADR-001…003 przyjęte: PostgreSQL, monolit warstwowy z modułami (struktura w ADR-002), JWT + refresh z rotacją (2026-11-15).
- [x] Specyfikacja slice'a 0 zaakceptowana (2026-11-15).
- [x] Slice 0 – kroki 1–7 planu: szkielet .NET, testy architektury, PostgreSQL + migracja, health checks, frontend, E2E, workflow CI (2026-11-15).
- [x] Slice 0 – push, PR do develop, zielony pipeline (kryterium 7) (2026-11-15).
- [x] Slice 0 – review (Claude; subagenci jeszcze nie istnieją): poprawki 1–3 wprowadzone (2026-11-15).
- [ ] Slice 0 – merge PR do develop (PO).

## Backlog z review slice'a 0
- Anonimowe `/health/ready` otwiera połączenie z bazą przy każdym wywołaniu → rate limiting (SYS-09, etap 8) lub krótki cache wyniku.
- Proxy Vite `'/health'` łapie każdą ścieżkę z tym prefiksem → zmienić na `'^/health/'` przy wprowadzeniu React Router.
- „Spróbuj ponownie” bez informacji zwrotnej podczas `isFetching` → stany ekranów (etap 7).
- `isHealthReport` akceptuje tablicę jako `checks` → przy kliencie z OpenAPI (SYS-07).
- Akcje GitHub przypięte do `@v4`, nie SHA → Dependabot (etap 8).
- README opisuje `.claude/agents`, `.claude/skills`, `.claude/settings.json`, których jeszcze nie ma (tydzień 4 wg build-plan.md).

## Otwarte pytania
- Slice 0: `.gitattributes` (końce linii) – opcjonalnie; skrypt `fsevents` celowo niezatwierdzony w npm 12; akcje GitHub przypięte do tagów `@v4`, nie SHA.
- Strategia testów (6 ścieżek E2E) i rozdział 12 (etapy 0–9) zostały tylko w spec-traintrack.md; rozbieżne z testing-strategy.md (5 ścieżek) i backlog.md – zdecydować, która wersja obowiązuje.
- Diagramy (ER, odświeżanie tokenu, architektura, mapa drogowa) nie przeszły eksportu – wyeksportować albo narysować w Mermaid.
- user-stories.md zawiera listę funkcji, a nie historie „Jako… chcę…”.
- Spec: ADR w docs/adr/ i frontend w web/; repo: docs/architecture/decisions/ i src/frontend.
- Anulowanie pakietu (BA-11) a prośby `Requested` – propozycja Claude: przechodzą w `Rejected`; decyzja PO.
- Powiadomienia (decyzja PO): czy korekta ujemna wyzwala „mało sesji”; czy klient dostaje powiadomienie o potwierdzeniu/odrzuceniu prośby; czy nowe zdarzenia (BA-04, BA-11, BA-21, BA-22) da się wyłączyć w K-04.
- Do potwierdzenia przez PO nazwy robocze w api.md: identyfikatory `/problems/...`, `notCountedReason`, `uphold`/`revert`, `paymentStatus`.
- Do potwierdzenia przez PO interpretacje Claude: styk sesji nie jest nakładaniem (BA-28), termin sporu = `StartsAt` + 7 dni (BA-20), ponowne użycie kodu zaproszenia → `409` (BA-26).

## Następny krok
Merge PR slice'a 0 do develop (PO). Potem ADR-004…006 i specyfikacja slice'a 1 (rejestracja).
