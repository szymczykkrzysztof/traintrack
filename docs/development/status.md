# Status prac

> Claude czyta ten plik na początku każdej sesji i aktualizuje go na końcu (skill `close-session`).

## Aktualny etap
Tydzień 1 – przygotowanie dokumentacji. Kodu aplikacji jeszcze nie ma.

## Zrobione
- [x] Struktura repozytorium, CLAUDE.md, protokół akceptacji, workflow, backlog, strategia testów.
- [x] Podział docs/product/spec-traintrack.md na pliki w docs/product/ i docs/architecture/ (2026-11-15).

## Do zrobienia teraz
- [ ] Sesja analityka: luki i sprzeczności w wymaganiach → docs/product/business-rules.md.
- [ ] ADR-001…003 (Claude przygotowuje, użytkownik decyduje).
- [ ] Doprecyzować i zaakceptować specs/foundation/00-walking-skeleton.md.

## Otwarte pytania
- Strategia testów (6 ścieżek E2E) i rozdział 12 (etapy 0–9) zostały tylko w spec-traintrack.md; rozbieżne z testing-strategy.md (5 ścieżek) i backlog.md – zdecydować, która wersja obowiązuje.
- Diagramy (ER, odświeżanie tokenu, architektura, mapa drogowa) nie przeszły eksportu – wyeksportować albo narysować w Mermaid.
- user-stories.md zawiera listę funkcji, a nie historie „Jako… chcę…”.
- Spec: ADR w docs/adr/ i frontend w web/; repo: docs/architecture/decisions/ i src/frontend.
- Na sesję analityka: reguła 1 (bez korekt) vs wzór w database.md (z korektami); domyślne liczenie ważności (reguła 3 vs dialog T-04).

## Następny krok
Druga sesja – analityk (patrz README.md, sekcja „Druga sesja – analityk”).
