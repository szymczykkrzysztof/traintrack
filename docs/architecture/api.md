# REST API

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 8 oraz „Endpointy autoryzacji” z rozdziału 7.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

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
| `POST /api/invitations/accept` | Client | Przyjęcie kodu; tworzy parę trener–klient |
| `GET /api/clients` | Trainer | Klienci z podsumowaniem pakietu; filtry `search`, `status=ToRenew` |
| `GET /api/clients/{clientId}` | Trainer | Karta klienta |
| `PATCH /api/clients/{clientId}` | Trainer | Archiwizacja klienta |

### Szablony, pakiety i płatności

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/package-templates` | Trainer | Własna oferta |
| `POST /api/package-templates` | Trainer | Nowy szablon |
| `PUT /api/package-templates/{id}` | Trainer | Edycja szablonu |
| `DELETE /api/package-templates/{id}` | Trainer | Dezaktywacja (szablon nie znika) |
| `GET /api/packages` | Client, Trainer, Admin | Lista w zakresie roli; filtry `status`, `clientId` |
| `GET /api/packages/{id}` | Client, Trainer, Admin | Szczegóły z `remaining`, `reserved`, `free` |
| `POST /api/packages` | Trainer | Nowy pakiet dla klienta (z szablonu lub ręcznie) |
| `POST /api/packages/{id}/adjustments` | Trainer | Korekta `delta` z `reason` |
| `POST /api/packages/{id}/extend` | Trainer | Nowa data ważności z powodem |
| `POST /api/packages/{id}/cancel` | Trainer | Anulowanie pakietu |
| `GET /api/packages/{id}/payments` | Client, Trainer | Wpłaty za pakiet |
| `POST /api/packages/{id}/payments` | Trainer | Zapis wpłaty |

### Sesje

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/sessions` | Client, Trainer | Filtry `from`, `to`, `clientId`, `packageId`, `status` |
| `POST /api/sessions` | Trainer | Zaplanowanie sesji; `409` gdy brak wolnych sesji lub nakładanie |
| `POST /api/sessions/request` | Client | Prośba o termin (trener potwierdza) |
| `PATCH /api/sessions/{id}` | Trainer | Zmiana terminu |
| `POST /api/sessions/{id}/complete` | Trainer | Oznaczenie jako odbyta |
| `POST /api/sessions/{id}/no-show` | Trainer | Nieobecność klienta |
| `POST /api/sessions/{id}/cancel` | Client, Trainer | Odwołanie; odpowiedź zawiera `consumesSession` |
| `POST /api/sessions/{id}/dispute` | Client | Zakwestionowanie odbytej sesji |
| `POST /api/sessions/{id}/resolve-dispute` | Trainer | Decyzja trenera: podtrzymaj lub cofnij |

### Karnety, pulpity i powiadomienia

| Endpoint | Kto | Opis |
| --- | --- | --- |
| `GET /api/memberships`, `POST /api/memberships` | Client | Lista i dodanie karnetu |
| `PUT /api/memberships/{id}`, `DELETE /api/memberships/{id}` | Client | Edycja i usunięcie |
| `PUT /api/memberships/{id}/sharing` | Client | Włączenie lub wyłączenie udostępniania trenerowi |
| `GET /api/clients/{clientId}/memberships` | Trainer | Tylko karnety udostępnione |
| `GET /api/dashboard/client` | Client | Dane pulpitu K-01 w jednym zapytaniu |
| `GET /api/dashboard/trainer` | Trainer | Dane pulpitu T-01 (liczniki, do odnowienia, dziś) |
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
