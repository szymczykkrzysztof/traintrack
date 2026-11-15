# REST API

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 8 oraz „Endpointy autoryzacji” z rozdziału 7.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.
> Zaktualizowano o decyzje BA-01…BA-33 z [business-rules.md](../product/business-rules.md) (2026-11-15); odwołania `BA-NN` wskazują źródło zmiany.

## 8. Projekt REST API

API jest zasobowe (REST) i zwraca JSON. Każdy endpoint, poza `/api/auth/register`, `/api/auth/login` i `/api/auth/refresh`, wymaga access tokenu. Kolumna „Kto” podaje role z uprawnieniem; zakres danych (własne lub swoich klientów) wynika z [rozdziału 2](../product/roles-and-permissions.md).

### Konwencje

| Obszar | Zasada |
| --- | --- |
| Format | JSON, nazwy pól `camelCase`, czas w ISO 8601 UTC (`2026-10-22T16:00:00Z`), daty bez godziny (`2026-11-30`) |
| Identyfikatory | `Guid` w ścieżce, np. `/api/packages/3f2a…` |
| Stronicowanie | `?page=1&pageSize=20`; odpowiedź `{ items, total, page, pageSize }`; maksymalnie 100 na stronę |
| Sortowanie i filtrowanie | `?sort=expiresOn,-remaining` (minus = malejąco) oraz filtry nazwane polami, np. `status=Active` |
| Błędy | Zawsze `application/problem+json` (RFC 9457): `type`, `title`, `status`, `detail`, `traceId`, a dla walidacji `errors: { pole: [komunikaty] }` |
| Współbieżność | Zasoby edytowalne zwracają `rowVersion`; zmiana wymaga przesłania go w treści lub w `If-Match`; niezgodność daje `409` |
| Dokumentacja | OpenAPI generowane przez `Microsoft.AspNetCore.OpenApi`, interaktywny widok (np. Scalar) w środowisku deweloperskim |
| Klient TypeScript | Generowany z OpenAPI (np. `openapi-typescript` lub `orval`), dzięki czemu typy frontendu zawsze zgadzają się z backendem |
| Wersjonowanie | Na start brak (`/api/...`); przy pierwszej zmianie łamiącej wprowadzamy `/api/v2` |

### Kody odpowiedzi

| Kod | Kiedy |
| --- | --- |
| `200`, `201`, `204` | Odczyt/zmiana; utworzenie (z nagłówkiem `Location`); akcja bez treści |
| `400` | Nieprawidłowy format lub walidacja pól |
| `401` | Brak lub wygasły access token |
| `403` | Rola nie ma uprawnienia |
| `404` | Zasób nie istnieje **lub należy do kogoś innego** |
| `409` | Naruszona reguła domeny (np. brak wolnych sesji, pakiet po terminie, konflikt wersji) |
| `429` | Przekroczony limit żądań (rate limiting) |

Reguły domeny mają stabilne identyfikatory w polu `type`, np. `/problems/no-free-sessions`, `/problems/package-expired`, `/problems/session-overlap`. Frontend pokazuje na ich podstawie właściwy komunikat po polsku.

Identyfikatory wynikające z decyzji BA (nazwy robocze, do potwierdzenia przy implementacji):

| `type` | Kiedy | Źródło |
| --- | --- | --- |
| `/problems/negative-remaining` | Korekta sprowadziłaby pozostałe sesje poniżej 0 | BA-02, BA-31 |
| `/problems/package-exhausted` | Rozliczenie sesji przy 0 pozostałych | BA-03 |
| `/problems/package-cancelled` | Zmiana pakietu `Cancelled` | BA-05 |
| `/problems/payment-required` | Planowanie przy `RequirePaymentFirst` w pakiecie nieopłaconym | BA-06, BA-08 |
| `/problems/payment-exceeds-price` | Suma aktywnych wpłat przekroczyłaby cenę | BA-09 |
| `/problems/session-not-started` | Oznaczenie sesji przed `StartsAt` | BA-23 |
| `/problems/dispute-window-closed` | Zakwestionowanie po `StartsAt` + 7 dni | BA-20 |
| `/problems/client-session-overlap` | Nakładanie się sesji klienta; bez szczegółów cudzej sesji | BA-28 |
| `/problems/client-has-active-package` | Archiwizacja klienta z pakietem `Active` | BA-25 |
| `/problems/relation-archived` | Zmiana danych zarchiwizowanego klienta | BA-32 |
| `/problems/invitation-not-acceptable` | Kod użyty, wygasły, własny albo relacja już aktywna | BA-26 |

### Endpointy autoryzacji

| Metoda i ścieżka | Wejście | Wynik |
| --- | --- | --- |
| `POST /api/auth/register` | e-mail, hasło, imię, opcjonalnie kod zaproszenia | `201`, konto z rolą Client |
| `POST /api/auth/login` | e-mail, hasło | `200`: access token w treści, refresh token w ciasteczku |
| `POST /api/auth/refresh` | ciasteczko z refresh tokenem | `200`: nowy access token + nowe ciasteczko; `401` przy błędzie |
| `POST /api/auth/logout` | ciasteczko | `204`, rodzina unieważniona, ciasteczko usunięte |
| `POST /api/auth/logout-all` | access token | `204`, wszystkie rodziny użytkownika unieważnione |
| `GET /api/auth/sessions` | access token | Lista urządzeń (rodzin) z ostatnią aktywnością |
| `DELETE /api/auth/sessions/{familyId}` | access token | Wylogowanie wskazanego urządzenia |
| `GET /api/auth/me` | access token | Profil, role, uprawnienia |

### Zaproszenia i klienci

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `POST /api/invitations` | Trainer | Tworzy zaproszenie; kod w odpowiedzi pokazany tylko raz |
| `GET /api/invitations` | Trainer | Lista własnych zaproszeń i ich statusów |
| `DELETE /api/invitations/{id}` | Trainer | Unieważnia zaproszenie |
| `POST /api/invitations/accept` | Client | Przyjęcie kodu; tworzy parę trener–klient albo przywraca zarchiwizowaną. Kod wiąże zaproszenie, nie adres e-mail; `409` dla kodu użytego, własnego albo przy aktywnej relacji (BA-26) |
| `GET /api/clients` | Trainer | Klienci z podsumowaniem pakietu; filtry `search`, `status` (`NoPackage`, `ToRenew`, `Active`, `Completed`, `Expired`, `Archived`). Status klienta i pokazywany pakiet wg BA-17, BA-18, BA-29; `Archived` – BA-32 |
| `GET /api/clients/{clientId}` | Trainer | Karta klienta; dla zarchiwizowanego tylko do odczytu, bez karnetów (BA-32) |
| `PATCH /api/clients/{clientId}` | Trainer | Archiwizacja klienta; `409`, gdy para ma pakiet `Active`. Cofa udostępnienia karnetów temu trenerowi (BA-25) |

### Szablony, pakiety i płatności

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/package-templates` | Trainer | Własna oferta |
| `POST /api/package-templates` | Trainer | Nowy szablon |
| `PUT /api/package-templates/{id}` | Trainer | Edycja szablonu |
| `DELETE /api/package-templates/{id}` | Trainer | Dezaktywacja (szablon nie znika) |
| `GET /api/packages` | Client, Trainer, Admin | Lista w zakresie roli; filtry `status`, `clientId` |
| `GET /api/packages/{id}` | Client, Trainer, Admin | Szczegóły z `remaining`, `reserved`, `free` (BA-01), `paymentStatus` (`Unpaid`, `PartiallyPaid`, `Paid`) i `paidAmount` (BA-06) |
| `POST /api/packages` | Trainer | Nowy pakiet dla klienta (z szablonu lub ręcznie). `validityStartsOn` domyślnie z ustawień trenera (BA-13); `cancellationWindowHours` kopiowany z ustawień (BA-12). Opcjonalne `initialPayment: { method }` zapisuje w tej samej transakcji wpłatę na pełną cenę z dzisiejszą datą w strefie trenera (BA-07) |
| `POST /api/packages/{id}/adjustments` | Trainer | Korekta `delta` z `reason`; `409`, gdy pozostałe spadłyby poniżej 0, licząc sesje `Disputed` jako zużyte (BA-02, BA-31). Może przywrócić `Completed` → `Active` (BA-05) |
| `POST /api/packages/{id}/extend` | Trainer | Nowa data ważności z powodem; może przywrócić `Expired` → `Active` (BA-05) |
| `POST /api/packages/{id}/cancel` | Trainer | Anulowanie pakietu z wymaganym `reason` (audyt). Sesje `Planned` → `CancelledByTrainer` bez zużycia, klient dostaje powiadomienie; wpłaty bez zmian. Nieodwracalne (BA-05, BA-11) |
| `GET /api/packages/{id}/payments` | Client, Trainer | Wpłaty za pakiet |
| `POST /api/packages/{id}/payments` | Trainer | Zapis wpłaty; `409`, gdy suma aktywnych wpłat przekroczyłaby cenę (BA-09) |
| `POST /api/packages/{id}/payments/{paymentId}/cancel` | Trainer | Anulowanie błędnej wpłaty z powodem (audyt); wpłata zostaje ze statusem `Cancelled` (BA-09) |

### Sesje

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/sessions` | Client, Trainer | Filtry `from`, `to`, `clientId`, `packageId`, `status` |
| `POST /api/sessions` | Trainer | Zaplanowanie sesji; `409` gdy brak wolnych sesji, nakładanie u trenera lub klienta (BA-28), termin po `ExpiresOn` albo nieopłacony pakiet przy `RequirePaymentFirst` (BA-08) |
| `POST /api/sessions/request` | Client | Prośba o termin: sesja `Requested`, rezerwuje miejsce; te same reguły co przy planowaniu (BA-22) |
| `POST /api/sessions/{id}/confirm` | Trainer | Potwierdzenie prośby: `Requested` → `Planned` (BA-22) |
| `POST /api/sessions/{id}/reject` | Trainer | Odrzucenie prośby z powodem: `Requested` → `Rejected` (BA-22) |
| `POST /api/sessions/{id}/withdraw` | Client | Wycofanie własnej prośby: `Requested` → `Withdrawn`; dla innych statusów `409` (BA-30) |
| `PATCH /api/sessions/{id}` | Trainer | Zmiana terminu; `409` dla terminu po `ExpiresOn` (BA-10) lub przy nakładaniu (BA-28) |
| `POST /api/sessions/{id}/complete` | Trainer | Oznaczenie jako odbyta; `409` przed `StartsAt` (BA-23) albo przy 0 pozostałych (BA-03). Dozwolone po wygaśnięciu pakietu dla sesji z terminem ≤ `ExpiresOn` (BA-10) |
| `POST /api/sessions/{id}/no-show` | Trainer | Nieobecność klienta; te same warunki co `complete` (BA-03, BA-10, BA-23) |
| `POST /api/sessions/{id}/cancel` | Client, Trainer | Odwołanie; odpowiedź zawiera `consumesSession`. Późne odwołanie klienta przy 0 pozostałych przechodzi bez zużycia, z `notCountedReason: "PackageExhausted"` (BA-04) |
| `POST /api/sessions/{id}/dispute` | Client | Zakwestionowanie sesji `Completed`, najpóźniej `StartsAt` + 7 dni; sesja `Disputed` nie zużywa, ale rezerwuje (BA-20) |
| `POST /api/sessions/{id}/resolve-dispute` | Trainer | Decyzja `uphold` (→ `Completed`) lub `revert` (→ `CancelledByTrainer`) z wymaganym `comment`; audyt i powiadomienie klienta (BA-21) |

### Karnety, pulpity i powiadomienia

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/memberships`, `POST /api/memberships` | Client | Lista i dodanie karnetu |
| `PUT /api/memberships/{id}`, `DELETE /api/memberships/{id}` | Client | Edycja i usunięcie; usunięcie jest miękkie (`Deleted`), bez przywracania (BA-27, BA-33) |
| `PUT /api/memberships/{id}/sharing` | Client | Lista trenerów, którym udostępniony jest karnet: `{ "trainerIds": [...] }`; tylko trenerzy z aktywną relacją, pusta lista = prywatny (BA-24) |
| `GET /api/clients/{clientId}/memberships` | Trainer | Tylko karnety udostępnione temu trenerowi, bez `Deleted` (BA-24, BA-27) |
| `GET /api/dashboard/client` | Client | Dane pulpitu K-01 w jednym zapytaniu |
| `GET /api/dashboard/trainer` | Trainer | Dane pulpitu T-01 (liczniki, do odnowienia, dziś, sesje w sporze – BA-21, sesje do rozliczenia – BA-23) |
| `GET /api/notifications` | zalogowany | Powiadomienia; filtr `unread=true` |
| `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all` | zalogowany | Oznaczenie jako przeczytane |
| `GET /api/notification-preferences`, `PUT /api/notification-preferences` | zalogowany | Ustawienia przypomnień |

### Administracja i operacje

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/admin/users` | Admin | Lista z wyszukiwaniem i filtrem roli |
| `PUT /api/admin/users/{id}/roles` | Admin | Ustawienie ról |
| `POST /api/admin/users/{id}/block`, `.../unblock`, `.../force-logout` | Admin | Blokada, odblokowanie, wymuszone wylogowanie |
| `GET /api/admin/audit` | Admin | Log audytu z filtrami |
| `GET /health/live`, `GET /health/ready` | publiczny | Sprawdzenia dla kontenera i monitoringu |

### Przykład: odwołanie sesji przez klienta

Klient odwołuje sesję 12 godzin przed startem. Próg trenera to 24 godziny, więc sesja zużywa pakiet, a odpowiedź mówi o tym wprost.

```http
POST /api/sessions/9c1e…/cancel
Authorization: Bearer <access token>
Content-Type: application/json

{ "reason": "Choroba" }
```

```json
{
  "id": "9c1e…",
  "status": "CancelledByClient",
  "consumesSession": true,
  "policy": { "cancellationWindowHours": 24, "hoursBeforeStart": 12 },
  "package": { "id": "3f2a…", "remaining": 1, "reserved": 0, "free": 1 }
}
```

Przykład błędu domeny przy próbie zaplanowania sesji bez wolnych miejsc:

```json
{
  "type": "/problems/no-free-sessions",
  "title": "Brak wolnych sesji w pakiecie",
  "status": 409,
  "detail": "Pakiet ma 2 sesje pozostałe, ale obie są już zarezerwowane.",
  "traceId": "00-4bf92f35…"
}
```
