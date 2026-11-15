# Architektura

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 9 oraz „Wymagania niefunkcjonalne” z rozdziału 11.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 9. Architektura i technologie

Aplikacja to **modularny monolit**: jeden backend .NET wystawiający REST API, jedna aplikacja jednostronicowa (SPA) w React i jedna baza PostgreSQL. Mikroserwisy byłyby tu przerostem formy, a monolit w czystych warstwach uczy tego, co przydaje się najczęściej w pracy: podziału odpowiedzialności, wstrzykiwania zależności i testowalności.

Diagram pokazuje kierunek zależności między projektami backendu.

&#91;embedded content: architektura backendu · 4 projekty, 2 usługi zewnętrzne\]

Api i Worker wywołują przypadki użycia z Application, Application korzysta z Domain, a Infrastructure implementuje interfejsy z Application i dopiero ona rozmawia z PostgreSQL i serwerem poczty. Dzięki temu reguły z [rozdziału 3](../product/business-rules.md) żyją w Domain i testuje się je bez bazy.

### Stos technologiczny

| Warstwa | Wybór | Czego się przy tym uczysz |
| --- | --- | --- |
| Runtime i język | .NET (aktualna wersja LTS), C# | Nowoczesny C#: rekordy, `async/await`, nullable reference types |
| API | Kontrolery `[ApiController]` (na start, bo dominują w istniejących projektach); refaktoryzacja do Minimal API jako zadanie dodatkowe | Routing, filtry, model binding, ProblemDetails |
| Dane | PostgreSQL, EF Core + Npgsql, migracje | Mapowanie, relacje, indeksy, transakcje, współbieżność |
| Uwierzytelnianie | ASP.NET Core Identity + własne JWT i refresh tokeny | Bezpieczeństwo w praktyce |
| Walidacja | FluentValidation | Walidacja poza kontrolerem |
| Zadania w tle | `BackgroundService` z `PeriodicTimer` na start; Hangfire lub Quartz.NET jako rozszerzenie | Zadania cykliczne, idempotencja |
| E-mail | MailKit (SMTP); lokalnie Mailpit | Integracja zewnętrzna |
| Logi i diagnostyka | Serilog, `ILogger`, health checks | Obserwowalność |
| Testy backendu | xUnit, Shouldly (lub AwesomeAssertions), NSubstitute, `WebApplicationFactory`, Testcontainers dla PostgreSQL, Bogus | Piramida testów, testy z prawdziwą bazą |
| Frontend | React + TypeScript + Vite | Komponenty, hooki, typowanie |
| Routing i dane | React Router, TanStack Query | Cache zapytań, stany ładowania i błędów |
| Formularze | React Hook Form + Zod | Walidacja po stronie klienta zgodna z API |
| Interfejs | Tailwind CSS + biblioteka komponentów (np. shadcn/ui) | Dostępność, układ responsywny |
| Testy frontendu | Vitest, React Testing Library, MSW, Playwright | Testy komponentów i end-to-end |
| Kontenery i CI | Docker Compose, GitHub Actions | Powtarzalne środowisko, automatyczne bramki jakości |

Bez rozstrzygania sporów o biblioteki: wybór jest propozycją do pierwszego commita, a decyzje zapisujemy w krótkich notatkach ADR (`docs/adr/0001-….md`).

### Struktura repozytorium

```text
traintrack/
├── src/
│   ├── TrainTrack.Domain/           encje, wartości, reguły; bez zależności
│   ├── TrainTrack.Application/      przypadki użycia, DTO, walidatory, interfejsy
│   ├── TrainTrack.Infrastructure/   EF Core, Identity, tokeny, e-mail, zadania
│   └── TrainTrack.Api/              kontrolery, konfiguracja, Program.cs
├── tests/
│   ├── TrainTrack.Domain.Tests/     testy jednostkowe reguł (najwięcej)
│   ├── TrainTrack.Application.Tests/
│   ├── TrainTrack.Api.IntegrationTests/   API + prawdziwy PostgreSQL
│   └── e2e/                         Playwright: kluczowe ścieżki użytkownika
├── web/
│   └── src/
│       ├── app/                     router, providery, układ
│       ├── features/                auth, packages, sessions, memberships, clients, admin
│       └── shared/                  komponenty UI, klient HTTP, pomocnicze hooki
├── docs/                            ta specyfikacja, ADR
├── docker-compose.yml               db, mailpit, api, web
└── .github/workflows/ci.yml
```

### Zasady w kodzie backendu

- **Domain:** encje mają zachowanie, a nie tylko pola (`package.ScheduleSession(...)`, `session.Complete(...)`). Reguły z [rozdziału 3](../product/business-rules.md) to małe, czyste metody, które testuje się bez bazy i bez frameworka.
- **Application:** jeden przypadek użycia to jedna klasa (np. `CompleteSessionHandler`). Błędy biznesowe zwracamy jako wynik (`Result<T>`), a nie rzucamy wyjątkami; wyjątki zostają dla sytuacji wyjątkowych.
- **Infrastructure:** implementuje interfejsy zdefiniowane w Application (`IPackageRepository`, `IEmailSender`, `ITokenService`). Konfiguracje encji w klasach `IEntityTypeConfiguration`.
- **Api:** cienkie kontrolery (przyjmij, wywołaj, zmapuj wynik); jedno miejsce zamienia `Result` i wyjątki na ProblemDetails.
- **Czas:** zawsze przez `TimeProvider`, nigdy `DateTime.UtcNow` w logice. Dzięki temu testy reguł 24 godzin i przypomnień są deterministyczne.
- **Konfiguracja:** wzorzec Options (`JwtOptions`, `ReminderOptions`), sekrety w `user-secrets` i zmiennych środowiskowych.

### Zasady w kodzie frontendu

- Podział według funkcji (`features/packages`, `features/sessions`), a nie według typu pliku; każda funkcja ma własne `api`, `hooks`, `components`, `pages`.
- Dane z serwera zarządza TanStack Query (cache, odświeżanie, stany `isPending` i `isError`); stan lokalny trzymamy w komponentach; globalny jest tylko kontekst uwierzytelniania.
- Jeden moduł `http` owija `fetch`: dokleja nagłówek `Authorization`, przy `401` uruchamia pojedyncze odświeżenie tokenu i powtarza zapytanie.
- Typy odpowiedzi pochodzą z wygenerowanego klienta OpenAPI, a schematy Zod dla formularzy mają te same reguły co walidatory backendu.
- Każdy ekran z [rozdziału 5](../product/screens.md) obsługuje stany: ładowanie, pusty, błąd.

### Środowisko lokalne i CI

1. `docker compose up` uruchamia PostgreSQL i Mailpit (podgląd wysłanych e-maili); API i Vite uruchamiamy z IDE lub terminala.
2. Dane demonstracyjne (administrator, trener Marek, klientka Anna, przykładowe pakiety) wgrywa seeder wyłącznie w środowisku `Development`.
3. Potok CI przy każdym pull requeście: budowanie, testy jednostkowe i integracyjne (Testcontainers), lint i testy frontendu, a na końcu Playwright na uruchomionym stosie.
4. Praca na krótkich gałęziach z pull requestami, nawet gdy repozytorium jest tylko Twoje: nawyk ten procentuje w zespole. Commity opisują funkcję identyfikatorem z [rozdziału 4](../product/user-stories.md).

## 11. Wymagania niefunkcjonalne (fragment rozdziału 11)

### Wymagania niefunkcjonalne

| Obszar | Wymaganie | Jak to sprawdzamy |
| --- | --- | --- |
| Wydajność | Listy i pulpity odpowiadają w czasie poniżej 300 ms (p95) przy 1 000 klientów na trenera i 50 000 sesji w bazie | Test obciążeniowy k6 na danych syntetycznych, plan zapytania (`EXPLAIN`) dla list |
| Dostępność interfejsu | Podstawy WCAG 2.1 AA: kontrast, obsługa klawiaturą, etykiety pól, kolor nigdy jedynym nośnikiem znaczenia | Automatyczne skany axe w Playwright + ręczny przegląd |
| Responsywność | Wszystkie ekrany klienta użyteczne od 360 px szerokości | Testy Playwright w emulacji telefonu |
| Niezawodność | Przypomnienia nie dublują się i nie giną po restarcie aplikacji | Testy zadań z `FakeTimeProvider`, test ponownego uruchomienia |
| Spójność danych | Licznik sesji zawsze zgodny z historią; brak podwójnej rezerwacji ostatniej sesji | Testy współbieżności (dwa równoległe żądania) |
| Obserwowalność | Logi strukturalne z `traceId`; kontrole `/health/live` i `/health/ready` | Przegląd logów w testach, sprawdzenie endpointów |
| Kopie zapasowe | Codzienny zrzut bazy i udokumentowane odtworzenie | Próbne odtworzenie na świeżej instancji |
| Lokalizacja | Interfejs po polsku, strefa `Europe/Warsaw`, waluta PLN; teksty w plikach zasobów (gotowość na EN) | Przegląd, brak twardych napisów w komponentach |
| Przeglądarki | Aktualne Chrome, Firefox, Safari i Edge | Playwright na trzech silnikach |
