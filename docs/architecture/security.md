# Bezpieczeństwo

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), „Jak to zapisać w kodzie” z rozdziału 2, rozdział 7 (endpointy auth są w [api.md](api.md)) oraz bezpieczeństwo i dane osobowe z rozdziału 11.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 2. Role i uprawnienia w kodzie (fragment rozdziału 2)

### Jak to zapisać w kodzie

- Uprawnienia mają postać stałych tekstowych, np. `packages.read`, `packages.create`, `sessions.complete`, `memberships.manage`, `users.manage`.
- Mapowanie rola → uprawnienia siedzi w jednym miejscu (klasa `Permissions` + seed w bazie), a w JWT trafiają tylko role.
- Uprawnienia sprawdza **policy-based authorization** w ASP.NET Core: `[Authorize(Policy = "packages.create")]`.
- Własność zasobu sprawdza osobny `IAuthorizationHandler` (np. `TrainerOwnsClientRequirement`), nie logika rozsiana po kontrolerach.
- Gdy użytkownik zna identyfikator cudzego zasobu, API zwraca `404`, a nie `403`, żeby nie ujawniać, że zasób istnieje.
- Po stronie React role służą tylko do ukrywania nawigacji i przycisków. Prawdziwa ochrona jest zawsze w API.

## 7. Uwierzytelnianie i autoryzacja: JWT, refresh token, RBAC

Użytkownik loguje się raz, a aplikacja co 15 minut po cichu odnawia krótkożyjący access token (JWT) za pomocą długożyjącego refresh tokenu. Refresh token jest jednorazowy: każde użycie wystawia nowy, a ponowne użycie starego unieważnia całą rodzinę tokenów danego urządzenia. To podejście jest wystarczająco realistyczne, żeby nauczyć się prawdziwych problemów (rotacja, wyścigi między kartami przeglądarki, kradzież tokenu), a jednocześnie wykonalne w kilka dni.

### Decyzje projektowe

| Element | Decyzja | Uzasadnienie |
| --- | --- | --- |
| Access token | JWT, ważny 15 minut, podpis HS256 na start (klucz z konfiguracji); później opcjonalnie RS256 lub ES256 | Krótki czas życia ogranicza szkodę po kradzieży; asymetryczny podpis to zadanie rozszerzające |
| Claims w JWT | `sub` (id użytkownika), `email`, `role` (jedna lub wiele), `jti`, `sid` (id rodziny refresh tokenu), `iss`, `aud`, `iat`, `exp` | Role w tokenie pozwalają autoryzować bez zapytania do bazy; `sid` wiąże token z urządzeniem |
| Refresh token | Losowy ciąg 32 bajtów (`RandomNumberGenerator`), kodowanie Base64Url; **nie JWT** | Nieprzezroczysty token da się unieważnić i nie zdradza treści |
| Przechowanie po stronie serwera | Tylko skrót SHA-256 tokenu w tabeli `RefreshToken` | Wyciek bazy nie ujawnia działających tokenów |
| Czas życia refresh tokenu | 14 dni przesuwane przy każdym użyciu, ale nie dłużej niż 30 dni od logowania | Aktywny użytkownik nie loguje się co tydzień, a sesja nie jest wieczna |
| Rotacja | Każde odświeżenie wydaje nową parę, stary token dostaje `UsedAt` i `ReplacedByTokenId` | Pozwala wykryć kradzież po ponownym użyciu |
| Wykrywanie ponownego użycia | Użyty już token pokazany ponownie → unieważnienie całej rodziny (`RevokedAt`) i wymuszone logowanie | Atakujący i prawdziwy użytkownik nie mogą jednocześnie korzystać z jednej sesji |
| Transport refresh tokenu | Ciasteczko `HttpOnly; Secure; SameSite=Strict; Path=/api/auth` | Skrypt na stronie (XSS) nie odczyta tokenu; ciasteczko jedzie tylko do endpointów auth |
| Przechowanie access tokenu w przeglądarce | Wyłącznie w pamięci aplikacji (zmienna), nigdy w `localStorage` | Po odświeżeniu strony aplikacja prosi o nowy access token przez `/auth/refresh` |
| Hasła | ASP.NET Core Identity (`PasswordHasher`), minimum 10 znaków, blokada po 5 błędnych próbach na 15 minut | Nie piszemy własnego hashowania |
| Tolerancja zegara | 30 sekund (`ClockSkew`) | Domyślne 5 minut jest zbyt luźne |

Uwaga praktyczna: ciasteczko `SameSite=Strict` jedzie do API tylko wtedy, gdy frontend i API należą do tej samej witryny. Dlatego lokalnie React (Vite) przekazuje ścieżkę `/api` do backendu przez proxy, a w produkcji oba działają za jednym serwerem pośredniczącym (reverse proxy) pod jedną domeną.

Endpointy autoryzacji opisuje [api.md](api.md#endpointy-autoryzacji).

### Przepływ odświeżania tokenu

Poniższy diagram pokazuje, co dzieje się, gdy access token wygaśnie w trakcie pracy.

&#91;embedded content: odświeżanie tokenu · 2 decyzje, 3 wyniki\]

Lewa kolumna to normalna praca: stary token zostaje oznaczony jako użyty, a klient dostaje nową parę i powtarza przerwane żądanie. Czerwony prostokąt to sygnał kradzieży, bo ten sam token pojawił się po raz drugi.

### Pułapki, które trzeba obsłużyć (i przetestować)

1. **Wyścig dwóch kart przeglądarki.** Dwie karty odświeżają ten sam refresh token niemal jednocześnie; druga pokazuje już użyty token i błędnie wygląda na atak. Rozwiązania: okno łaski (np. 10 sekund, w którym ponowne użycie zwraca ten sam wynik), a po stronie React blokada `navigator.locks` lub kanał `BroadcastChannel`.
2. **Wiele równoległych zapytań z wygasłym tokenem.** Interceptor w kliencie HTTP uruchamia jedno odświeżenie, a pozostałe zapytania czekają na jego wynik (wzorzec single-flight).
3. **Zmiana roli.** Nowe role widać dopiero w kolejnym access tokenie, czyli z opóźnieniem do 15 minut. Jest to świadomy kompromis, a awaryjnie administrator wymusza wylogowanie.
4. **Wylogowanie a access token.** Wylogowany użytkownik ma jeszcze przez najwyżej 15 minut ważny access token. Dla większej szczelności (zadanie rozszerzające) API sprawdza `sid` na liście unieważnionych sesji w pamięci podręcznej.
5. **Wyliczanie użytkowników.** Logowanie, rejestracja i reset hasła zwracają ten sam ogólny komunikat bez względu na to, czy konto istnieje.
6. **Sekrety.** Klucz podpisu trafia do `dotnet user-secrets` lokalnie i do zmiennych środowiskowych w produkcji, nigdy do repozytorium.

### RBAC w praktyce

- **Backend (.NET):** `AddAuthentication().AddJwtBearer(...)` z `MapInboundClaims = false`, żeby claimy `sub` i `role` zachowały swoje nazwy. Domyślna polityka wymaga zalogowanego użytkownika (`FallbackPolicy`), a endpointy `auth` oznaczamy `[AllowAnonymous]`. Uprawnienia z [rozdziału 2](../product/roles-and-permissions.md) stają się politykami (`AddAuthorization`), a własność zasobu sprawdza `IAuthorizationHandler`.
- **Frontend (React):** kontekst `AuthProvider` trzyma access token w pamięci i dane użytkownika; hak `useAuth()` udostępnia role; komponent `<RequireRole roles={["Trainer"]}>` chroni trasy i ukrywa elementy interfejsu. Ochrona w React jest tylko wygodą, a bezpieczeństwo zapewnia wyłącznie API.
- **Testy:** dla każdej pary (endpoint, rola) powstaje test integracyjny sprawdzający `200` / `403` / `404` oraz test, że trener A nie widzi klienta trenera B.

## 11. Bezpieczeństwo i dane osobowe (fragment rozdziału 11)

### Bezpieczeństwo (lista kontrolna według OWASP Top 10)

| Zagrożenie | Środek | Test |
| --- | --- | --- |
| Błędna kontrola dostępu (IDOR) | Polityki + sprawdzanie własności zasobu; `404` dla cudzych | Test: trener A prosi o zasób trenera B; klient o dane innego klienta |
| Niepoprawne uwierzytelnianie | Rotacja refresh tokenów, blokada konta, limit prób | Testy z [rozdziału 7](#7-uwierzytelnianie-i-autoryzacja-jwt-refresh-token-rbac) (ponowne użycie, wygaśnięcie) |
| Wstrzykiwanie | Zapytania parametryzowane przez EF Core; brak sklejania SQL | Przegląd kodu, analiza statyczna |
| XSS | React escapuje treść; access token tylko w pamięci; nagłówek CSP | Test nagłówków odpowiedzi |
| CSRF | Ciasteczko `SameSite=Strict`, ograniczone do `/api/auth`; wymagany nagłówek niestandardowy | Test żądania z obcego źródła |
| Błędna konfiguracja | Nagłówki bezpieczeństwa (HSTS, `X-Content-Type-Options`), CORS tylko dla znanego źródła, brak szczegółów błędów w produkcji | Test integracyjny nagłówków |
| Podatne zależności | Dependabot, `dotnet list package --vulnerable`, `npm audit` w CI | Bramka w potoku |
| Ujawnianie danych w logach | Brak haseł, tokenów i pełnych e-maili w logach | Test sprawdzający logi z logowania |
| Nadużycia | Rate limiting na `login`, `refresh`, `register`, zaproszeniach | Test: seria żądań kończy się `429` |

### Dane osobowe

Aplikacja przechowuje dane osobowe (imię, e-mail, historia treningów, wpłaty). W projekcie szkoleniowym wystarczy potraktować to poważnie i zbudować podstawy: zgoda i polityka prywatności przy rejestracji, eksport własnych danych (JSON), usunięcie lub anonimizacja konta oraz wgląd admina zapisywany w logu audytu. To nie jest porada prawna; przy wdrożeniu produkcyjnym wymagane są konsultacje.
