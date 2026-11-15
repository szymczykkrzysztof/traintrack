# Backlog i harmonogram MVP

Kolejność wynika z zależności: bez konta nie ma relacji trener–klient, bez relacji nie ma pakietu,
bez pakietu nie ma licznika.

## Vertical slices

| # | Slice | Warstwy | Kluczowe kryteria akceptacji | Spec |
| --- | --- | --- | --- | --- |
| 0 | Walking skeleton | React → `/health` → PostgreSQL, CI | Strona pokazuje status API i bazy; pipeline zielony | specs/foundation/00-walking-skeleton.md |
| 1 | Rejestracja | Formularz → `POST /auth/register` → Identity | E-mail unikalny; hasło min. 10 znaków; rola Client domyślnie | specs/auth/01-register.md |
| 2 | Logowanie i odświeżanie | Login → JWT + refresh → interceptor | Rotacja tokenu; ponowne użycie starego tokenu unieważnia rodzinę | specs/auth/02-login.md |
| 3 | Zaproszenie klienta | Trener: e-mail → kod → rejestracja klienta | Kod jednorazowy, ważny 7 dni, w bazie tylko skrót | specs/trainer-client/01-invite.md |
| 4 | Oferta trenera | CRUD szablonów pakietów | Zmiana oferty nie zmienia sprzedanych pakietów | specs/packages/01-offer.md |
| 5 | Przypisanie pakietu | Trener → pakiet dla klienta + płatność | Pakiet kopiuje warunki; brak dostępu do cudzego klienta (403/404) | specs/packages/02-assign.md |
| 6 | Pulpit klienta | Licznik, ważność, najbliższy trening | Pozostałe = suma − odbyte − nieobecności ± korekty; widoczne w 3 s | specs/packages/03-counter.md |
| 7 | Planowanie i odbycie sesji | Kalendarz trenera, statusy | Odwołanie poza oknem nie zużywa sesji; brak sesji ponad pakiet | specs/sessions/01-plan.md |
| 8 | Odwołanie, zakwestionowanie i przepięcie | Klient odwołuje / kwestionuje; trener przepina sesję (TRN-16) | Każda zmiana statusu w historii i logu audytu; przepięcie podlega regułom planowania w pakiecie docelowym (BA-03) | specs/sessions/03-cancel.md |
| 9 | Karnety na siłownię | CRUD klienta + udostępnienie trenerowi | Trener nie widzi karnetu bez zgody | specs/memberships/01-crud.md |
| 10 | Przypomnienia | `BackgroundService` raz dziennie + e-mail + w aplikacji | Jedno przypomnienie na próg; dzień w strefie użytkownika | specs/reminders/01-daily-job.md |
| 11 | Pulpit trenera | Lista klientów „do odnowienia” | Pakiet z ≤2 treningami lub ≤14 dniami trafia na listę | specs/trainer-client/03-dashboard.md |
| 12 | Admin i audyt (po MVP) | Użytkownicy, blokada, log | Wgląd admina jest logowany | – |

Specyfikacje slice'ów 1–11 Claude tworzy w kroku 1 cyklu (docs/development/workflow.md),
na podstawie specyfikacji produktu w docs/product/.

## Harmonogram (ok. 8–10 h tygodniowo, orientacyjnie)

| Tydzień | Zakres | Definition of Done |
| --- | --- | --- |
| 1 | Podział specyfikacji na docs/, sesja analityka, ADR-001…003 | Dokumentacja w repo, zaakceptowane ADR-y |
| 2 | ADR-004…006, Foundation, slice 0 | `docker compose up` → React pokazuje status API i bazy; CI zielone |
| 3 | Slice 1–2 | Testy tokenu i E2E logowania |
| 4 | Slice 3; hooki w .claude/settings.json | Trener zaprasza klienta end-to-end |
| 5 | Slice 4–5 | Testy izolacji danych trenerów |
| 6 | Slice 6 | Kryterium „3 sekundy”, testy spójności licznika |
| 7 | Slice 7–8 | Test współbieżności, log audytu |
| 8 | Slice 9–11 | Zadanie w tle z `TimeProvider`, testy strefy czasowej |
| 9 | Hardening | 5 ścieżek E2E, OWASP API Top 10, obsługa błędów, rate limiting logowania |
| 10 | Delivery + sprawdzian | Demo online, monitoring, README; sprawdzian z review |

## ADR-y do przygotowania

1. ADR-001 PostgreSQL zamiast SQL Server.
2. ADR-002 Modularny monolit zamiast mikroserwisów.
3. ADR-003 JWT + refresh token z rotacją zamiast sesji cookie.
4. ADR-004 Licznik sesji wyliczany z historii, a nie przechowywany jako pole.
5. ADR-005 Zadania w tle: `BackgroundService` vs Hangfire/Quartz.
6. ADR-006 Kontrakty API: OpenAPI + generowanie typów TypeScript.

Claude przygotowuje każdy ADR z porównaniem opcji; użytkownik podejmuje decyzję.

## Sprawdzian w tygodniu 10

- Claude wprowadza na osobnej gałęzi 3 ukryte błędy (logika licznika, autoryzacja,
  test bez asercji). Użytkownik znajduje je w review.
- Użytkownik opowiada w 10 minut, jak żądanie „oznacz sesję jako odbytą” przechodzi przez aplikację.
