# ADR-003: JWT i refresh token z rotacją zamiast sesji cookie

- **Status:** Accepted
- **Data:** 2026-11-15
- **Decyduje:** użytkownik (Product Owner); przygotowuje: Claude

## Kontekst
- **Architektura:** SPA w React i API w ASP.NET Core działają pod jedną domeną (proxy Vite lokalnie, reverse proxy w produkcji – [security.md](../security.md)).
- **Wymagania:**
  - logowanie e-mailem i hasłem przez ASP.NET Core Identity;
  - wylogowanie z jednego urządzenia i ze wszystkich;
  - lista aktywnych urządzeń (AUTH-05, AUTH-10, S-03);
  - wymuszone wylogowanie przez admina (ADM-03);
  - autoryzacja ról i własności zasobu (rozdział 2).
- **Cel szkoleniowy:** CLAUDE.md i specyfikacja zakładają naukę JWT, rotacji i wykrywania kradzieży tokenu. Aplikacja mobilna jest poza zakresem.

## Rozważane opcje

| Opcja | Zalety | Wady |
| --- | --- | --- |
| A. Sesja cookie ASP.NET Core Identity (`HttpOnly` cookie, stan sesji po stronie serwera lub w zaszyfrowanym cookie) | Najprostsza; skrypt (XSS) nie odczyta poświadczeń; wylogowanie i zmiana ról działają natychmiast przez security stamp; mniej własnego kodu bezpieczeństwa | Wymaga ochrony CSRF dla wszystkich zmieniających endpointów (SameSite + antiforgery); słabo pasuje do przyszłych klientów spoza przeglądarki; nie realizuje celu szkoleniowego (JWT, rotacja) |
| B. Bearer tokeny z `MapIdentityApi` (wbudowane w .NET) | Gotowe endpointy login/refresh, mało kodu | Tokeny nieprzezroczyste w formacie Identity, nie JWT; brak rodzin tokenów, wykrywania ponownego użycia i listy urządzeń – AUTH-04 i AUTH-10 trzeba by i tak dopisać; refresh token trafia do JavaScriptu |
| C. Własne JWT (15 min, w pamięci SPA) + nieprzezroczysty refresh token z rotacją w cookie `HttpOnly; SameSite=Strict; Path=/api/auth` (projekt z security.md) | Pełna kontrola: rodziny tokenów = urządzenia, wykrywanie kradzieży, okno łaski dla wielu kart; autoryzacja bez zapytania do bazy; zgodne ze specyfikacją i celem szkoleniowym | Najwięcej własnego kodu bezpieczeństwa (więcej testów); zmiana ról i wylogowanie działają z opóźnieniem do 15 minut (świadomy kompromis z security.md); pułapki wielu kart i równoległych odświeżeń trzeba obsłużyć w SPA |

## Rekomendacja Claude
**Opcja C**, z jednym uczciwym zastrzeżeniem.

Gdyby celem był wyłącznie produkt, poleciłbym A. Dla SPA pod jedną domeną sesja cookie jest prostsza, a pod względem bezpieczeństwa co najmniej równie dobra. Ten projekt ma jednak dwa cele, a drugim jest nauka (rozdział 1 specyfikacji). Opcja C:
- realizuje AUTH-03, AUTH-04 i AUTH-10 bez obchodzenia frameworka;
- trzyma refresh token poza zasięgiem JavaScriptu, więc łączy główną zaletę A z modelem tokenowym;
- jest już szczegółowo zaprojektowana w [security.md](../security.md).

Ryzyko „własnego kodu bezpieczeństwa” ograniczamy w trzech miejscach:
- hasła, lockout i hashowanie zostają w Identity;
- JWT obsługują standardowe biblioteki (`Microsoft.AspNetCore.Authentication.JwtBearer`);
- każdą pułapkę z security.md pokrywa test.

## Decyzja
**Opcja C – własne JWT (15 min, w pamięci SPA) + nieprzezroczysty refresh token z rotacją w cookie `HttpOnly`**, zgodnie z projektem w security.md. Decyzja Product Ownera z 2026-11-15, zgodna z rekomendacją Claude.

## Konsekwencje
- **Zyskujemy:**
  - model urządzeń (rodzina tokenów) dla S-03 i wymuszonego wylogowania;
  - wykrywanie kradzieży refresh tokenu;
  - autoryzację bez zapytania do bazy przy każdym żądaniu.
- **Tracimy:**
  - natychmiastowe działanie zmiany ról i wylogowania: opóźnienie do 15 min, chyba że wdrożymy listę unieważnionych `sid` (rozszerzenie z security.md).
- **Do zrobienia:**
  - ADR nie zmienia slice'a 0 (bez uwierzytelniania); dotyczy slice'ów 1–2;
  - klucz podpisu w `user-secrets` / zmiennych środowiskowych;
  - testy integracyjne: rotacja, ponowne użycie, okno łaski, wygaśnięcie 30 dni;
  - test E2E odświeżania w przeglądarce;
  - cookie z refresh tokenem jest ograniczone do `/api/auth` i `SameSite=Strict`, ale endpointy `auth` i tak potrzebują ochrony CSRF zgodnie z listą OWASP w security.md (nagłówek niestandardowy).
