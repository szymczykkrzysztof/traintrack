# TrainTrack – plan budowy z Claude Code

Nov 11, 2026 · @Krzysztof

## 1. Cel planu i punkt wyjścia

**TrainTrack** to aplikacja dla trenera personalnego i osoby trenującej: wspólny licznik treningów w pakiecie, historia sesji, karnety na siłownię i przypomnienia przed wygaśnięciem. Specyfikację produktu (zakres MVP, role i RBAC, model danych, API, architektura, testy) przygotowaliśmy już w dokumencie *Specyfikacja: aplikacja do śledzenia treningów i karnetów*.

Ten plan odpowiada na inne pytanie: **jak tę aplikację zbudować z Claude Code tak, żeby po drodze stać się developerem**, a nie operatorem generatora kodu. Zasada nadrzędna:

> Claude Code jest programistą w Twoim zespole. Ty odpowiadasz za architekturę, decyzje, jakość i odbiór. Nie akceptujesz kodu, którego nie potrafisz wytłumaczyć.

### Stos technologiczny (z ustaleń)

| Warstwa | Technologia |
| --- | --- |
| Backend | ASP.NET Core Web API (.NET), modularny monolit |
| Dane | PostgreSQL + EF Core (code-first, migracje) |
| Uwierzytelnianie | ASP.NET Core Identity, JWT + refresh token z rotacją |
| Frontend | React + TypeScript + Vite, React Query |
| Testy | xUnit, WebApplicationFactory + Testcontainers, Vitest + RTL, Playwright |
| Infrastruktura | Docker Compose, GitHub Actions |

### Definicja ukończenia całego planu

- Działające publiczne demo z kontami demo (trener, dwóch klientów).
- Zielony pipeline CI: build, lint, testy jednostkowe, integracyjne i E2E krytycznych ścieżek.
- Dokumentacja w repozytorium (`docs/`, ADR-y, README) aktualna względem kodu.
- Rozumiesz i potrafisz wytłumaczyć każdy zaakceptowany fragment kodu i testów (sprawdzian w tygodniu 10).

## 2. Model pracy

Sześć technik, które się uzupełniają, a nie konkurują:

| Technika | Jak wygląda w TrainTrack |
| --- | --- |
| Spec-Driven Development | Każdy slice ma plik `specs/<obszar>/<slice>.md` z user story, regułami i kryteriami akceptacji, zanim powstanie kod |
| Vertical Slice Development | Budujemy całe funkcje (UI → API → DB → testy), np. „klient widzi licznik pakietu”, a nie „całe API” |
| Test-Driven Development | Claude zamienia kryteria akceptacji w testy przed implementacją; Ty akceptujesz listę przypadków i same testy po objaśnieniu |
| AI-Assisted Pair Programming | Claude implementuje, Ty prowadzisz review, pytasz „dlaczego”, odrzucasz lub zatwierdzasz |
| Incremental Delivery | Każdy slice kończy się działającym, zmergowanym fragmentem aplikacji |
| Continuous Integration | GitHub Actions uruchamia build i testy na każdym PR; czerwony pipeline blokuje merge |

Oficjalny schemat Anthropic **Explore → Plan → Code → Commit** jest kręgosłupem cyklu z rozdziału 7.

### Podział odpowiedzialności

| Ty – Product Owner i recenzent | Claude Code – zespół developerski |
| --- | --- |
| Ustalasz priorytety i zakres slice'a | Pisze specyfikacje, plany, cały kod produkcyjny i wszystkie testy |
| Wybierasz wariant architektury po objaśnieniu | Przygotowuje ADR-y z porównaniem opcji i kompromisów |
| Akceptujesz listę przypadków testowych, dopisujesz brakujące | Pisze i uruchamia testy, pokazuje, że failują z właściwego powodu |
| Akceptujesz, odrzucasz lub dopytujesz o każdy krok | Objaśnia każdą zmianę w stałym formacie i odpowiada na pytania |
| Sprawdzasz działanie w aplikacji, robisz merge | Prowadzi niezależne przeglądy przez subagentów, aktualizuje dokumentację |

### Proporcja czasu (orientacyjnie)

- 15% – specyfikacja, priorytety i decyzje,
- 40% – czytanie kodu i objaśnień, zadawanie pytań,
- 20% – przypadki testowe i przegląd testów,
- 15% – ręczna weryfikacja w działającej aplikacji,
- 10% – dziennik nauki i retrospektywa.

Cel: akceptacja oznacza zrozumienie, a nie klikanie „akceptuj”.

## 3. Etapy cyklu życia

Część dokumentów już mamy w specyfikacji — w repozytorium przenosimy je do plików Markdown, żeby Claude Code czytał je w każdej sesji.

| Etap | Co robimy w TrainTrack | Rezultat | Status |
| --- | --- | --- | --- |
| Discovery | Problem: niewiadoma liczba treningów, wygasające karnety, spory „był trening czy nie” | `docs/product/product-brief.md` | Gotowe w specyfikacji (rozdz. 1) |
| Requirements | User stories dla Klienta, Trenera, Admina; reguły pakietu i odwołań | `user-stories.md`, `business-rules.md` | Gotowe w specyfikacji, do rozbicia na `specs/` |
| Architecture | Modularny monolit, model danych, API, bezpieczeństwo | `docs/architecture/*`, ADR-001…ADR-006 | Do spisania jako ADR-y |
| Foundation | Repo, szkielet backend/frontend, Docker, CI, pierwszy test | Działający szkielet „hello health” | Tydzień 2–3 |
| Implementation | Kolejne slice'y z rozdziału 6 | Kompletne funkcje MVP | Tydzień 3–8 |
| Hardening | E2E, bezpieczeństwo, wydajność, obsługa błędów | Release Candidate | Tydzień 9 |
| Delivery | Wdrożenie, monitoring, README, portfolio | Publiczne demo | Tydzień 10 |

### ADR-y do napisania w tygodniu 1–2

Claude przygotowuje każdy ADR (kontekst, rozważane opcje, rekomendacja, konsekwencje) i objaśnia kompromisy. Ty zadajesz pytania i podejmujesz decyzję — dopiero wtedy ADR dostaje status *Accepted*.

1. ADR-001 PostgreSQL zamiast SQL Server (koszt hostingu, Testcontainers, `xmin` jako token współbieżności).
2. ADR-002 Modularny monolit zamiast mikroserwisów.
3. ADR-003 JWT + refresh token z rotacją zamiast cookie session.
4. ADR-004 Licznik sesji wyliczany z historii, a nie przechowywany jako pole (spójność danych).
5. ADR-005 Zadania w tle: `BackgroundService` vs Hangfire/Quartz dla przypomnień.
6. ADR-006 Kontrakty API: OpenAPI + generowanie typów TypeScript.

## 4. Repozytorium i CLAUDE.md

### Struktura

```
traintrack/
├── CLAUDE.md
├── README.md
├── docker-compose.yml
├── docs/
│   ├── product/        product-brief.md, user-stories.md, business-rules.md
│   ├── architecture/   overview.md, database.md, api.md, security.md, decisions/ADR-00x.md
│   └── development/    coding-standards.md, testing-strategy.md, status.md, review-protocol.md
├── specs/
│   ├── auth/           01-register.md, 02-login.md, 03-refresh.md
│   ├── trainer-client/ 01-invite.md, 02-accept.md
│   ├── packages/       01-offer.md, 02-assign.md, 03-counter.md
│   ├── sessions/       01-plan.md, 02-complete.md, 03-cancel.md
│   ├── memberships/    01-crud.md
│   └── reminders/      01-daily-job.md
├── src/
│   ├── backend/        TrainTrack.Api, TrainTrack.Modules.*, TrainTrack.Shared
│   └── frontend/       Vite + React + TS (features/, api/, components/)
├── tests/
│   ├── TrainTrack.UnitTests/
│   ├── TrainTrack.IntegrationTests/
│   └── e2e/            Playwright + TypeScript
├── .claude/
│   ├── settings.json   uprawnienia i hooki
│   ├── skills/
│   └── agents/
└── .github/workflows/ci.yml
```

`docs/development/status.md` to „pamięć” między sesjami: co zrobione, co otwarte, następny krok. Claude aktualizuje go na koniec każdej sesji.

### CLAUDE.md (do 200 linii)

```markdown
# TrainTrack – instrukcje projektu

## Produkt
Aplikacja dla trenera personalnego i klienta: pakiety treningów,
historia sesji, karnety na siłownię, przypomnienia o wygaśnięciu.
Szczegóły: docs/product/, aktualny stan: docs/development/status.md

## Stos
- Backend: ASP.NET Core Web API, modularny monolit
- Dane: PostgreSQL + EF Core (code-first, migracje)
- Auth: Identity + JWT + refresh token z rotacją
- Frontend: React + TypeScript + Vite + React Query
- Testy: xUnit, Testcontainers, Vitest, RTL, Playwright

## Komendy
- docker compose up -d db
- dotnet build / dotnet test
- cd src/frontend && npm run lint && npm test
- cd tests/e2e && npx playwright test

## Reguły domenowe (nie łamią ich żadne zmiany)
- Pozostałe treningi wynikają z historii sesji i korekt, nie z pola licznika.
- Trener widzi tylko swoich klientów, klient tylko swoje dane.
- Karnet klienta jest prywatny, chyba że klient go udostępnił.
- Dane rozliczeniowe nie są usuwane, tylko zmieniają status.
- Czas zdarzeń w UTC, daty kalendarzowe jako DateOnly, strefa z konta.

## Zasady pracy
- Najpierw plan, potem kod. Zmiany znaczące czekają na akceptację.
- Małe, skupione zmiany; bez refaktoryzacji niezwiązanego kodu.
- Każda zmiana schematu = migracja EF Core.
- Każda reguła biznesowa ma test; każdy endpoint ma test autoryzacji.
- Każdy krok kończ objaśnieniem wg docs/development/review-protocol.md
  i czekaj na moje „akceptuję” przed commitem.
- Wyjaśniaj ważne decyzje, jakbyś mentorował mid .NET developera.

## Quality gates
Build, testy unit + integration, lint frontendu, E2E krytycznych ścieżek.
```

### docs/development/review-protocol.md

Najważniejszy plik w tym modelu pracy: opisuje, jak Claude objaśnia zmiany i jak Ty je akceptujesz.

```markdown
# Protokół objaśniania i akceptacji

## Po każdym kroku Claude podaje
1. Co zmieniłem (pliki, jednym zdaniem każdy).
2. Dlaczego tak i jakie były alternatywy.
3. Przepływ żądania: React → API → serwis → EF Core → PostgreSQL.
4. Nowe pojęcia .NET / React / testowe użyte po raz pierwszy.
5. Ryzyka: na co patrzeć w review.
6. Jak sprawdzić samemu (komenda, klik w UI).
7. Trzy pytania kontrolne do mnie.

## Moje odpowiedzi
- akceptuję – commit i następny krok
- wyjaśnij: <co> – głębsze objaśnienie, bez zmian w kodzie
- zmień: <co> – poprawka, potem nowe objaśnienie
- odrzucam: <dlaczego> – wycofanie kroku, nowa propozycja

## Zasady
- Jeden krok planu na raz, potem stop.
- Nic nie trafia do commita bez „akceptuję”.
- Testy przed implementacją; pokaż, że failują z właściwego powodu.
```

## 5. Skills, subagenci i hooki

Dodajesz je **dopiero po pierwszym slice'ie** (tydzień 4), gdy wiesz, które czynności się powtarzają. Wcześniej zbudowałbyś automatyzację procesu, którego jeszcze nie znasz.

| Mechanizm | Odpowiada na pytanie | TrainTrack |
| --- | --- | --- |
| CLAUDE.md | Jak działa projekt i jakie są zasady? | Rozdział 4 |
| Skills | Jak wykonać dany proces? | Lista poniżej |
| Subagenci | Komu zlecić niezależną analizę? | 4 role tylko do odczytu |
| Hooki | Co ma się dziać automatycznie? | Format, build, blokady |

### Skills (`.claude/skills/`)

- `write-spec` – zamienia user story w plik `specs/...md` z regułami, kryteriami, scenariuszami negatywnymi i bezpieczeństwa.
- `vertical-slice` – pełny cykl z rozdziału 7 z bramkami akceptacji po każdym kroku.
- `test-cases` – tabela przypadków testowych (pozytywne, negatywne, brzegowe, autoryzacja) do akceptacji przed kodem testów.
- `explain-change` – objaśnienie bieżącego diffu w formacie z `review-protocol.md`.
- `api-endpoint` – konwencje modułu: endpoint, walidacja, autoryzacja zasobowa, ProblemDetails, test integracyjny.
- `ef-migration` – zmiana encji, migracja, objaśnienie wygenerowanego SQL, test na Testcontainers.
- `playwright-e2e` – Page Objects, dane testowe przez API, bez sztywnych `wait`.
- `close-session` – aktualizacja `status.md`, dziennika nauki, otwartych problemów i następnych kroków.

### Subagenci (`.claude/agents/`, wszyscy bez prawa zapisu)

| Agent | Zakres przeglądu |
| --- | --- |
| `architect` | Granice modułów, zgodność z ADR, zależności |
| `backend-reviewer` | C#, EF Core, autoryzacja zasobowa, walidacja, spójność licznika |
| `frontend-reviewer` | React, TypeScript, stany ładowania/błędu, dostępność |
| `qa-engineer` | Pokrycie kryteriów akceptacji, brakujące scenariusze negatywne i brzegowe |

Zasada: główna sesja implementuje, subagenci tylko recenzują. Równoległa edycja tych samych plików przez kilku agentów kończy się konfliktami.

### Hooki (`.claude/settings.json`)

- Po edycji `*.cs` – `dotnet format` dla zmienionego pliku.
- Po edycji `*.ts(x)` – `eslint --fix` dla zmienionego pliku.
- Przed komendą – blokada `git push --force`, `rm -rf`, `dotnet ef database drop`.
- Uprawnienia: migracje, zmiany w `.github/` i `docker-compose.yml` zawsze wymagają Twojej zgody.

## 6. Backlog vertical slices

Każdy slice to jeden branch, jeden PR i działająca funkcja od UI do bazy. Kolejność wynika z zależności: bez konta nie ma relacji trener–klient, bez relacji nie ma pakietu, bez pakietu nie ma licznika.

| # | Slice | Warstwy | Kluczowe kryteria akceptacji |
| --- | --- | --- | --- |
| 0 | Walking skeleton | React → `/health` → PostgreSQL, CI | Strona pokazuje status API i bazy; pipeline zielony |
| 1 | Rejestracja | Formularz → `POST /auth/register` → Identity | E-mail unikalny; hasło min. 10 znaków; rola Client domyślnie |
| 2 | Logowanie i odświeżanie | Login → JWT + refresh → interceptor | Rotacja tokenu; ponowne użycie starego tokenu unieważnia rodzinę |
| 3 | Zaproszenie klienta | Trener: e-mail → kod → rejestracja klienta | Kod jednorazowy, ważny 7 dni, w bazie tylko skrót |
| 4 | Oferta trenera | CRUD szablonów pakietów | Zmiana oferty nie zmienia sprzedanych pakietów |
| 5 | Przypisanie pakietu | Trener → pakiet dla klienta + płatność | Pakiet kopiuje warunki; trener nie przypisze pakietu cudzemu klientowi (403/404) |
| 6 | Pulpit klienta | Licznik, ważność, najbliższy trening | Pozostałe = suma − odbyte − nieobecności ± korekty; widoczne w 3 s |
| 7 | Planowanie i odbycie sesji | Kalendarz trenera, statusy | Odwołanie poza oknem nie zużywa sesji; w oknie – wg ustawień; brak sesji ponad pakiet |
| 8 | Odwołanie i zakwestionowanie | Klient odwołuje / kwestionuje | Każda zmiana statusu w historii i logu audytu |
| 9 | Karnety na siłownię | CRUD klienta + udostępnienie trenerowi | Trener nie widzi karnetu bez zgody |
| 10 | Przypomnienia | `BackgroundService` raz dziennie + e-mail + w aplikacji | Jedno przypomnienie na próg; dzień liczony w strefie użytkownika |
| 11 | Pulpit trenera | Lista klientów „do odnowienia” | Pakiet z ≤2 treningami lub ≤14 dniami trafia na listę |
| 12 | Admin i audyt | Użytkownicy, blokada, log | Wgląd admina jest logowany |

### Szablon pliku specyfikacji slice'a

```markdown
# Slice 6 – Pulpit klienta
## User story
Jako klient chcę po zalogowaniu widzieć, ile treningów mi zostało
i do kiedy ważny jest pakiet, żeby nie być zaskoczonym końcem pakietu.
## Reguły biznesowe
## Kryteria akceptacji (Given / When / Then)
## Scenariusze negatywne i brzegowe
## Bezpieczeństwo i autoryzacja
## Poza zakresem
## Testy: unit / integration / E2E
```

Plan na 10 tygodni obejmuje slice'y 0–11. Slice 12 i pozycje „później” ze specyfikacji (Google login, kalendarz tygodniowy, raport CSV) to backlog po MVP.

## 7. Cykl pracy nad jednym slice'em

Claude pisze cały kod i wszystkie testy. Ty masz bramkę akceptacji po każdym kroku: najpierw akceptujesz zamiar (spec, plan, przypadki testowe), potem wynik (testy, kod). Nic nie trafia do commita bez Twojego „akceptuję”.

| Krok | Claude | Ty | Bramka |
| --- | --- | --- | --- |
| 1. Spec | Pisze `specs/.../NN-nazwa.md` z kryteriami akceptacji | Uzupełniasz reguły biznesowe | Akceptacja specyfikacji |
| 2. Plan | Plan Mode: pliki, kroki, zależności, ryzyka | Oceniasz kolejność i zakres | Akceptacja planu |
| 3. Przypadki testowe | Tabela przypadków: pozytywne, negatywne, brzegowe, autoryzacja | Dopisujesz brakujące — tu działa Twoje doświadczenie QA | Akceptacja listy |
| 4. Testy | Pisze testy, uruchamia, pokazuje czerwone wyniki i objaśnia | Czytasz testy, sprawdzasz asercje | Akceptacja testów |
| 5. Implementacja | Jeden krok planu, zielone testy, objaśnienie | Czytasz diff i objaśnienie, dopytujesz | Akceptacja kroku |
| 6. Weryfikacja | Build, wszystkie testy, uruchomienie aplikacji | Sprawdzasz działanie w UI | Potwierdzenie działania |
| 7. Review niezależny | Subagenci recenzują diff, Claude priorytetyzuje znaleziska | Decydujesz, co poprawić teraz, a co do backlogu | Decyzja o znaleziskach |
| 8. Commit + PR | Commit, opis PR, `status.md`, wpis do dziennika | Merge do `develop` | Merge |

### Prompt 1 – Plan

```
Read CLAUDE.md, docs/development/status.md and specs/packages/03-counter.md.
Explore the codebase in Plan Mode. Do not modify any files.
Prepare an implementation plan split into small steps: affected modules,
entities, endpoints, components, dependencies and risks.
Explain the plan so I can accept or change it.
```

### Prompt 2 – Przypadki testowe

```
Based on the acceptance criteria, list test cases in a table:
level (unit / integration / E2E), scenario, input, expected result.
Include negative, boundary and authorization cases.
Do not write test code yet. Wait for my acceptance.
```

### Prompt 3 – Testy

```
Write the accepted test cases. Do not implement production code.
Run them and show they fail for the right reason.
Explain each test: what it protects, why this assertion,
how test data is isolated. Wait for my acceptance.
```

### Prompt 4 – Implementacja krok po kroku

```
Implement step 1 of the accepted plan only. Follow existing conventions,
no unrelated refactoring. Run the relevant tests.
Then explain the change using docs/development/review-protocol.md
and stop. Do not commit until I write: akceptuję.
```

### Prompt 5 – Niezależny review

```
Ask the backend-reviewer and qa-engineer agents to independently review
the current git diff against the spec. No agent may modify files.
Report actual defects, grouped by severity, each with a failure scenario.
Explain each finding in plain terms and recommend: fix now or backlog.
```

### Prompt 6 – Pogłębienie

```
Explain [plik / metoda / linia] deeper, as if mentoring a junior .NET developer.
Show what happens step by step at runtime, what would break if we removed it,
and link the relevant Microsoft docs. Do not change any code.
```

### Plan Mode – kiedy obowiązkowo

| Zadanie | Sposób pracy |
| --- | --- |
| Nowy slice | Plan → przypadki testowe → testy → implementacja |
| Zmiana architektury, nowy moduł | Plan Mode obowiązkowo + ADR |
| Zmiana schematu DB | Plan → migracja → objaśnienie SQL → testy |
| Bug | Test odtwarzający błąd → poprawka → objaśnienie przyczyny |
| Drobna zmiana UI | Krótkie objaśnienie, bez rozbudowanego planu |

## 8. Strategia testów

Testy pisze Claude, ale Ty — jako QA — jesteś ich recenzentem: akceptujesz listę przypadków przed kodem testów i sprawdzasz, czy każdy test naprawdę może upaść. Nie akceptuj sytuacji, w której testowanie kończy się na kilku testach jednostkowych.

| Poziom | Narzędzia | Co testujemy w TrainTrack | Ile |
| --- | --- | --- | --- |
| Unit (backend) | xUnit | Licznik pakietu, okno odwołania, wyliczanie `ExpiresOn`, progi przypomnień | Najwięcej |
| Unit (frontend) | Vitest | Formatowanie dat w strefie, mapowanie statusów | Średnio |
| Komponenty | React Testing Library | Pulpit klienta: ładowanie, błąd, pusty stan, dane | Średnio |
| Integracja | xUnit + WebApplicationFactory + Testcontainers (PostgreSQL) | Endpointy, migracje, autoryzacja zasobowa, współbieżność | Dużo |
| Kontrakt | OpenAPI + generowane typy TS | Zgodność DTO backend–frontend (build failuje przy rozjeździe) | Automatycznie |
| E2E | Playwright + TS | 5 krytycznych ścieżek (poniżej) | Najmniej |

### Testy, które muszą istnieć

- **Izolacja danych:** dla każdego endpointu z zasobem – trener B nie odczyta i nie zmieni pakietu klienta trenera A; klient nie zobaczy cudzego karnetu.
- **Spójność licznika:** pozostałe treningi zawsze równe wyliczeniu z historii, także po korektach i odwołaniach.
- **Współbieżność:** dwie równoczesne próby oznaczenia ostatniej sesji jako odbytej – jedna przechodzi, druga dostaje 409.
- **Czas:** test granicy dnia (23:30 w Warszawie vs UTC) i zmiany czasu letniego – przez wstrzykiwany `TimeProvider`.
- **Tokeny:** ponowne użycie zrotowanego refresh tokenu unieważnia całą rodzinę.

### Krytyczne ścieżki E2E

1. Trener zaprasza klienta → klient rejestruje się z kodem → obaj widzą relację.
2. Trener przypisuje pakiet → klient widzi licznik i ważność.
3. Trener planuje i oznacza sesję → licznik klienta maleje.
4. Klient odwołuje sesję poza oknem → licznik bez zmian.
5. Klient dodaje karnet, udostępnia go → trener go widzi.

Zasady: dane testowe zakładane przez API, a nie przez UI; Page Object Model; zero `waitForTimeout`; prawdziwy PostgreSQL zamiast mocków bazy.

## 9. Git, CI/CD i kontrola agenta

### Branche

```
main                 ← tylko wydania (tag v0.x)
└── develop          ← integracja, zawsze zielona
     ├── feature/00-walking-skeleton
     ├── feature/01-register
     ├── feature/06-client-dashboard
     └── ...
```

Commity małe, w konwencji Conventional Commits (`feat(packages): ...`, `test(auth): ...`). Każdy slice = PR z opisem: link do specyfikacji, co zrobiono, jak testować, ryzyka.

### Pipeline GitHub Actions

| Job | Kroki | Kiedy |
| --- | --- | --- |
| backend | restore → build → unit → integration (Testcontainers) | Każdy PR |
| frontend | npm ci → lint → typecheck → Vitest → build | Każdy PR |
| e2e | docker compose up → Playwright → raport HTML jako artefakt | PR do `develop` |
| deploy | build obrazów → wdrożenie demo | Merge do `main` |

### Pięć zasad kontroli

1. **Małe kroki** – jedno polecenie = jeden krok planu, potem stop i objaśnienie.
2. **Akceptacja = zrozumienie** – bez objaśnienia i Twojego „akceptuję” nic nie trafia do commita.
3. **Git po każdym kroku** – łatwy powrót, czytelny diff.
4. **Ograniczona autonomia** – migracje, infrastruktura, CI i operacje destrukcyjne zawsze za Twoją zgodą.
5. **Kontrola kontekstu** – nowy slice = nowa sesja; źródłem prawdy są `docs/`, `specs/` i `status.md`, nie historia czatu. Na koniec sesji skill `close-session`.

## 10. Harmonogram MVP (10 tygodni)

Zakładam ok. 8–10 godzin pracy tygodniowo. To orientacyjny plan, nie gwarancja — jeśli slice się przeciąga, przesuwasz kolejne, a nie skracasz testy.

| Tydzień | Zakres | Rezultat / Definition of Done |
| --- | --- | --- |
| 1 | Claude przenosi specyfikację do `docs/`, przygotowuje ADR-001…003; Ty decydujesz | Repo z dokumentacją, CLAUDE.md, `review-protocol.md`, zaakceptowane ADR-y |
| 2 | ADR-004…006, Foundation, slice 0 | `docker compose up` → React pokazuje status API i bazy; CI zielone |
| 3 | Slice 1–2: rejestracja, logowanie, refresh | Testy tokenu i E2E logowania |
| 4 | Slice 3: zaproszenia; skills `explain-change`, `test-cases`, subagenci | Trener zaprasza klienta end-to-end; `.claude/` gotowe |
| 5 | Slice 4–5: oferta i przypisanie pakietu | Testy izolacji danych trenerów |
| 6 | Slice 6: pulpit klienta (licznik) | Kryterium „3 sekundy” spełnione, testy spójności licznika |
| 7 | Slice 7–8: sesje, odwołania, zakwestionowanie | Test współbieżności, log audytu |
| 8 | Slice 9–11: karnety, przypomnienia, pulpit trenera | Zadanie w tle z `TimeProvider`, testy strefy czasowej |
| 9 | Hardening | 5 ścieżek E2E, przegląd bezpieczeństwa (OWASP API Top 10), obsługa błędów, rate limiting logowania |
| 10 | Delivery + sprawdzian | Wdrożenie demo, monitoring i logi, README; **sprawdzian z review** (rozdział 11) |

### Rytuał tygodniowy (30 min, np. w niedzielę)

- Co zostało zmergowane i czy CI jest zielone?
- Czego się nauczyłem (3 punkty do `docs/learning/journal.md`)?
- Który fragment kodu Claude nadal jest dla mnie niejasny?
- Zakres i slice na kolejny tydzień.

## 11. Jak akceptować kod i testy, żeby się uczyć

W tym modelu nie piszesz kodu, więc cała nauka odbywa się w review. Zasada: **akceptujesz tylko to, co potrafisz własnymi słowami wytłumaczyć**. Jeśli nie potrafisz — odpowiadasz „wyjaśnij: …”, a nie „akceptuję”.

### Checklista akceptacji kodu

1. Czy umiem jednym zdaniem powiedzieć, co robi ta zmiana?
2. Czy wiem, skąd przychodzą dane i dokąd trafiają (React → API → baza)?
3. Czy wiem, co się stanie przy błędzie, braku danych i braku uprawnień?
4. Czy zmiana trzyma się planu i nie dotyka niezwiązanych plików?
5. Czy istnieje test, który upadłby, gdyby ten kod był błędny?

### Checklista akceptacji testów (Twoje doświadczenie QA)

- Nazwa testu opisuje zachowanie biznesowe, nie metodę.
- Czytelne Arrange / Act / Assert; asercja na wynik, nie na szczegóły implementacji.
- Test był czerwony przed implementacją.
- Dane izolowane, brak `sleep` i zależności od kolejności.
- Są scenariusze negatywne, brzegowe i autoryzacji.
- **Test mutacyjny na żądanie:** „Temporarily break rule X and show which test fails, then revert.” Jeśli żaden test nie upada — test jest do poprawy.

### Na co patrzysz w review w kolejnych slice'ach

| Slice | Na co zwracasz uwagę | Czego się uczysz |
| --- | --- | --- |
| 0 | `Program.cs`: rejestracja DbContext, health checks | DI, konfiguracja, pipeline middleware |
| 1 | Walidacja rejestracji i format błędów | Walidacja, ProblemDetails |
| 2 | Interceptor odświeżania tokenu we frontendzie | Asynchroniczność, wyścigi żądań |
| 3 | Generowanie i hashowanie kodu zaproszenia | Kryptografia w .NET |
| 5 | Handler autoryzacji zasobowej | `IAuthorizationHandler`, polityki |
| 6 | Wyliczanie pozostałych sesji, zapytanie EF i jego SQL | Modelowanie domeny, LINQ → SQL |
| 6 | Komponent licznika i test RTL | Stan, React Query, testy komponentów |
| 7 | Reguła okna odwołania | Czas, `TimeProvider`, TDD |
| 8 | Migracja dla logu audytu | Migracje, przegląd SQL |
| 10 | `BackgroundService` przypomnień | Hosted services, scope DI, idempotencja |

### Dziennik nauki

Po każdym slice'ie Claude dopisuje szkic do `docs/learning/journal.md`, a Ty go poprawiasz: trzy nowe pojęcia, jedna decyzja, której sam byś nie podjął, i otwarte pytania. Na końcu sesji Claude zadaje 3 pytania kontrolne z całego slice'a.

### Sprawdzian w tygodniu 10

- Claude wprowadza w osobnym branchu 3 ukryte błędy (logika licznika, autoryzacja, test bez asercji). Ty znajdujesz je w samym review.
- Opowiadasz w 10 minut, jak żądanie „oznacz sesję jako odbytą” przechodzi przez całą aplikację — to dobry materiał do portfolio i na rozmowę rekrutacyjną.

## 12. Pierwsze kroki w tym tygodniu

- [ ] Zainstaluj .NET SDK (LTS), Node.js LTS, Docker Desktop, Git i Claude Code.
- [ ] Załóż puste repozytorium `traintrack` na GitHubie i sklonuj je.
- [ ] Pierwsza sesja Claude Code: „Read the plan below. Create the repository structure, CLAUDE.md and docs/development/review-protocol.md. Explain every file and wait for my acceptance before committing.” (wklej rozdziały 4 i 7).
- [ ] Claude przenosi specyfikację do `docs/product/` i `docs/architecture/` — Ty czytasz i akceptujesz.
- [ ] Sesja analityka: Claude zadaje pytania o luki w wymaganiach, po jednym; odpowiedzi trafiają do `business-rules.md`.
- [ ] Claude przygotowuje ADR-001…003 z porównaniem opcji; Ty decydujesz.
- [ ] Claude rozpisuje `specs/foundation/00-walking-skeleton.md`; akceptujesz kryteria.

Potem tydzień 2 zaczynasz od Prompt 1 (Plan) dla slice'a 0.
