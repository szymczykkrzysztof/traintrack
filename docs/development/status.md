# Status prac

> Claude czyta ten plik na początku każdej sesji i aktualizuje go na końcu (skill `close-session`).

## Aktualny etap
Tydzień 1 – przygotowanie dokumentacji. Kodu aplikacji jeszcze nie ma.

## Zrobione
- [x] Struktura repozytorium, CLAUDE.md, protokół akceptacji, workflow, backlog, strategia testów.
- [x] Podział docs/product/spec-traintrack.md na pliki w docs/product/ i docs/architecture/ (2026-11-15).
- [x] Sesja analityka: 33 decyzje BA-01…BA-33 w docs/product/business-rules.md (2026-11-15).

## Do zrobienia teraz
- [ ] Wprowadzić punkty „Do uzupełnienia” z BA-01…BA-33 do dokumentów:
  - [x] database.md: `Payment.Status`, `Package.CancellationWindowHours`, status `GymMembership`, tabela udostępnień karnetu, nakładanie sesji klienta;
  - [x] api.md: wpłata początkowa w `POST /api/packages`, anulowanie wpłaty, `reason` przy anulowaniu pakietu, potwierdzenie i odrzucenie prośby, lista trenerów w `sharing`;
  - [x] notifications.md: próg z pozostałych sesji, strefa trenera, nowe powiadomienia (późne odwołanie, anulowanie pakietu, rozstrzygnięcie sporu, nowa prośba);
  - [ ] screens.md: T-01 (spory, do rozliczenia), T-02 (Ewa, filtr Zarchiwizowani), T-04 (metoda płatności), K-03 (wybór trenerów);
  - [x] business-rules.md: statusy `Requested`, `Rejected`, `Withdrawn` w „Statusy sesji”.
- [ ] Backlog: nowa funkcja „przepięcie sesji do innego pakietu / na jednorazową” (BA-03) bez ID.
- [ ] ADR-001…003 (Claude przygotowuje, użytkownik decyduje).
- [ ] Doprecyzować i zaakceptować specs/foundation/00-walking-skeleton.md.

## Otwarte pytania
- Strategia testów (6 ścieżek E2E) i rozdział 12 (etapy 0–9) zostały tylko w spec-traintrack.md; rozbieżne z testing-strategy.md (5 ścieżek) i backlog.md – zdecydować, która wersja obowiązuje.
- Diagramy (ER, odświeżanie tokenu, architektura, mapa drogowa) nie przeszły eksportu – wyeksportować albo narysować w Mermaid.
- user-stories.md zawiera listę funkcji, a nie historie „Jako… chcę…”.
- Spec: ADR w docs/adr/ i frontend w web/; repo: docs/architecture/decisions/ i src/frontend.
- Anulowanie pakietu (BA-11) a prośby `Requested` – propozycja Claude: przechodzą w `Rejected`; decyzja PO.
- Powiadomienia (decyzja PO): czy korekta ujemna wyzwala „mało sesji”; czy klient dostaje powiadomienie o potwierdzeniu/odrzuceniu prośby; czy nowe zdarzenia (BA-04, BA-11, BA-21, BA-22) da się wyłączyć w K-04.
- Do potwierdzenia przez PO nazwy robocze w api.md: identyfikatory `/problems/...`, `notCountedReason`, `uphold`/`revert`, `paymentStatus`.
- Do potwierdzenia przez PO interpretacje Claude: styk sesji nie jest nakładaniem (BA-28), termin sporu = `StartsAt` + 7 dni (BA-20), ponowne użycie kodu zaproszenia → `409` (BA-26).

## Następny krok
Wprowadzić „Do uzupełnienia” z business-rules.md do dokumentów architektury i ekranów, potem ADR-001…003.
