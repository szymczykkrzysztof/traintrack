# Model danych

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 6.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 6. Model danych

Baza: **PostgreSQL**, dostęp przez **EF Core** (podejście code-first z migracjami). Poniżej encje, ich kluczowe pola i reguły integralności. Nazwy w kodzie po angielsku (`Package`, `Session`), w interfejsie po polsku.

&#91;embedded content: diagram ER · 9 głównych encji\]

Pakiet (wyróżniony) jest środkiem modelu: należy do pary trener–klient, może powstać z szablonu, a sesje, korekty i płatności wiszą na nim. Sesja ma `PackageId` opcjonalny, bo zdarza się trening jednorazowy poza pakietem. Diagram nie pokazuje wszystkich pól ani encji `Invitation`, `NotificationPreference`, `ReminderLog`, `Notification`, `AuditLog` i `TrainerSettings`; pełna lista jest w tabelach poniżej.

### Zasady przekrojowe

- Klucze główne: `Guid` (v7, jeśli dostępny w .NET, dla dobrej lokalności indeksów).
- Czas zdarzeń (`StartsAt`, `CreatedAt`) w UTC (`timestamptz`); daty kalendarzowe (`ExpiresOn`, `EndsOn`) jako `DateOnly`. Strefa użytkownika (domyślnie `Europe/Warsaw`) jest polem konta i służy do wyświetlania oraz do liczenia „dnia” w przypomnieniach.
- Pieniądze: `decimal(10,2)` + kod waluty (domyślnie `PLN`), nigdy `double`.
- Brak twardego usuwania danych rozliczeniowych: pakiety, sesje i płatności zmieniają status zamiast znikać; konto używa flagi `IsActive`.
- Każda tabela biznesowa ma `CreatedAt`, a zmienne `UpdatedAt` oraz token współbieżności (w PostgreSQL kolumna systemowa `xmin`).

### Konto i tokeny

| Encja | Kluczowe pola | Uwagi |
| --- | --- | --- |
| `User` (ASP.NET Core Identity) | `Id`, `Email`, `PasswordHash`, `FullName`, `TimeZoneId`, `IsActive`, `LockoutEnd` | Hasła hashuje Identity (PBKDF2); e-mail unikalny |
| `Role`, `UserRole` | role `Admin`, `Trainer`, `Client` | Seed przy starcie aplikacji |
| `RefreshToken` | `Id`, `UserId`, `FamilyId`, `TokenHash`, `ExpiresAt`, `UsedAt`, `RevokedAt`, `ReplacedByTokenId`, `UserAgent`, `CreatedByIp` | W bazie tylko skrót SHA-256 tokenu; jedna rodzina = jedno urządzenie ([rozdział 7](security.md)) |
| `AuditLog` | `At`, `ActorId`, `Action`, `EntityType`, `EntityId`, `DataJson`, `Ip` | Tylko do dopisywania, bez edycji |

### Treningi i pakiety

| Encja | Kluczowe pola | Uwagi |
| --- | --- | --- |
| `TrainerClient` | `TrainerId`, `ClientId`, `Status`, `CreatedAt` | Unikalna para (`TrainerId`, `ClientId`); bez niej trener nie widzi klienta |
| `Invitation` | `TrainerId`, `Email`, `CodeHash`, `ExpiresAt`, `AcceptedAt`, `AcceptedByUserId` | Kod jednorazowy, ważny np. 7 dni, w bazie tylko skrót |
| `TrainerSettings` | `TrainerId`, `CancellationWindowHours`, `ValidityStartsOn`, `RequirePaymentFirst` | Domyślne wartości dla nowych pakietów |
| `PackageTemplate` | `TrainerId`, `Name`, `SessionCount`, `Price`, `Currency`, `ValidityDays`, `SessionMinutes`, `IsActive` | Oferta; zmiana nie dotyka sprzedanych pakietów |
| `Package` | `TrainerClientId`, `TemplateId?`, `Name`, `TotalSessions`, `Price`, `PurchasedOn`, `ValidityDays`, `ExpiresOn?`, `Status` | Kopia warunków z chwili zakupu; `ExpiresOn` bywa puste do pierwszej sesji |
| `PackageAdjustment` | `PackageId`, `Delta`, `Reason`, `CreatedBy`, `CreatedAt` | Ręczna korekta (np. +1 sesja bonusowa); powód wymagany |
| `Session` | `PackageId?`, `TrainerId`, `ClientId`, `StartsAt`, `DurationMinutes`, `Status`, `ConsumesSession`, `CancelledAt`, `CancelledBy`, `Note` | `PackageId` puste dla sesji jednorazowej; `ConsumesSession` ustawiane w chwili zmiany statusu |
| `Payment` | `PackageId`, `Amount`, `PaidOn`, `Method`, `Note`, `CreatedBy` | Ręczny zapis wpłat, bez bramki płatniczej |

### Karnety i powiadomienia

| Encja | Kluczowe pola | Uwagi |
| --- | --- | --- |
| `GymMembership` | `ClientId`, `ClubName`, `Type`, `StartsOn`, `EndsOn`, `EntriesTotal?`, `EntriesUsed?`, `Price`, `SharedWithTrainers` | Własność klienta; `SharedWithTrainers` domyślnie fałsz |
| `NotificationPreference` | `UserId`, `Kind`, `EmailEnabled`, `InAppEnabled`, `ThresholdsJson` | Progi, np. `[14,7,1]` dni |
| `ReminderLog` | `UserId`, `Kind`, `SubjectId`, `Threshold`, `Channel`, `SentAt` | Unikalny indeks (`UserId`, `Kind`, `SubjectId`, `Threshold`, `Channel`) gwarantuje, że przypomnienie nie wyjdzie dwa razy |
| `Notification` | `UserId`, `Title`, `Body`, `Link`, `CreatedAt`, `ReadAt` | Powiadomienia w aplikacji (dzwonek) |

Dochodzi jeszcze kolejka `OutboxMessage` (`Type`, `PayloadJson`, `CreatedAt`, `ProcessedAt`, `Attempts`, `Error`), w której czekają e-maile do wysłania ([rozdział 10](notifications.md)).

### Jak liczyć pozostałe sesje

```text
Pozostało = Package.TotalSessions
          + SUM(PackageAdjustment.Delta)
          − COUNT(Session  WHERE PackageId = p AND ConsumesSession = true)

Zarezerwowane = COUNT(Session WHERE PackageId = p AND Status = 'Planned')
Wolne do planowania = Pozostało − Zarezerwowane
```

Liczba nie jest kolumną. Dla list (np. u trenera) liczymy ją w jednym zapytaniu z grupowaniem albo w widoku bazodanowym, a w testach sprawdzamy, że widok i logika domenowa dają ten sam wynik.

### Ograniczenia i indeksy

1. `UNIQUE (TrainerId, ClientId)` na `TrainerClient`; `UNIQUE (TokenHash)` na `RefreshToken`.
2. Indeksy pod najczęstsze zapytania: `Session (TrainerId, StartsAt)`, `Session (ClientId, StartsAt)`, `Package (TrainerClientId, Status)`, `Package (ExpiresOn)` dla zadania wygaszającego, `GymMembership (EndsOn)` dla przypomnień.
3. `CHECK (TotalSessions > 0)`, `CHECK (ValidityDays > 0)`, `CHECK (EndsOn >= StartsOn)`.
4. Planowanie sesji odbywa się w transakcji, która blokuje wiersz pakietu (`SELECT … FOR UPDATE`) albo opiera się na tokenie współbieżności. Dzięki temu dwie równoległe prośby nie zarezerwują ostatniej wolnej sesji naraz.
5. Dwie sesje tego samego trenera nie mogą się nakładać w czasie (wyjątkowe ograniczenie `EXCLUDE USING gist` na zakresie czasu albo sprawdzenie w serwisie z testem).
