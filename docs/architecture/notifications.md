# Powiadomienia i zadania w tle

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 10.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 10. Powiadomienia i zadania w tle

Przypomnienia są główną wartością aplikacji, więc muszą być przewidywalne: wysyłają się na czas, raz i do właściwych osób. Część zdarzeń sprawdza zadanie cykliczne (daty), a część reaguje natychmiast na akcję użytkownika (licznik sesji).

### Zdarzenia i ich wyzwalacze

| Zdarzenie | Jak wykrywane | Warunek | Klucz unikalności (`ReminderLog`) |
| --- | --- | --- | --- |
| Mało sesji w pakiecie | Natychmiast po oznaczeniu sesji jako odbytej, nieobecnej lub późno odwołanej | `free` ≤ próg (domyślnie 2) i pakiet `Active` | (`UserId`, `PackageLow`, `PackageId`, próg) |
| Zbliża się ważność pakietu | Zadanie dzienne | `ExpiresOn − dziś` ∈ {14, 7} i zostały sesje | (`UserId`, `PackageExpiring`, `PackageId`, liczba dni) |
| Pakiet wygasł | Zadanie dzienne (wygaszanie) | `ExpiresOn < dziś`, status zmieniony na `Expired` | (`UserId`, `PackageExpired`, `PackageId`, 0) |
| Zbliża się koniec karnetu | Zadanie dzienne | `EndsOn − dziś` ∈ progi użytkownika (domyślnie 14, 7, 1) | (`UserId`, `MembershipExpiring`, `MembershipId`, liczba dni) |
| Trening jutro | Zadanie godzinne | sesja `Planned` startuje za 23–24 godziny | (`UserId`, `SessionTomorrow`, `SessionId`, 24) |
| Zakwestionowana sesja | Natychmiast | klient użył „Zakwestionuj sesję” | (`TrainerId`, `SessionDisputed`, `SessionId`, 0) |

Do każdego klucza dochodzi kanał (e-mail lub aplikacja). Odbiorcy wynikają z [rozdziału 3](../product/business-rules.md): pakiet dotyczy klienta i trenera, karnet klienta (a trenera tylko wtedy, gdy karnet jest udostępniony).

### Zadania cykliczne

| Zadanie | Harmonogram | Co robi |
| --- | --- | --- |
| `ExpirationJob` | raz dziennie, 06:30 `Europe/Warsaw` | Zmienia status przeterminowanych pakietów na `Expired` i zapisuje zdarzenie |
| `ReminderJob` | raz dziennie, 07:00 `Europe/Warsaw` | Generuje przypomnienia o datach (pakiety, karnety) |
| `SessionReminderJob` | co godzinę | Generuje przypomnienia o jutrzejszych sesjach |
| `OutboxSenderJob` | co minutę | Wysyła zaległe wiadomości z kolejki (`OutboxMessage`), ponawia przy błędzie |

Na start każde zadanie to `BackgroundService` z `PeriodicTimer`. Gdy działa, można je przenieść na Hangfire lub Quartz.NET, żeby poznać różnicę między własnym kodem a gotowym planistą. Rozszerzenie: godzina przypomnień według strefy czasowej użytkownika (`TimeZoneId`).

### Jak uniknąć duplikatów i utraty wiadomości

1. **Idempotencja.** Zadanie najpierw próbuje wstawić wiersz do `ReminderLog` (unikalny indeks). Jeśli wstawienie się nie uda, przypomnienie już wyszło i zadanie je pomija. Dzięki temu ponowne uruchomienie zadania (awaria, wdrożenie) niczego nie dubluje.
2. **Kolejka (outbox).** Zadanie nie wysyła e-maila bezpośrednio. W jednej transakcji zapisuje `ReminderLog`, `Notification` (dzwonek) i `OutboxMessage` (e-mail). Osobne zadanie wysyła wiadomości i ponawia próby z rosnącym odstępem; po 5 nieudanych próbach oznacza wiadomość jako błędną.
3. **Szanowanie ustawień.** Przed zapisaniem wiadomości zadanie sprawdza `NotificationPreference` odbiorcy (kanał e-mail i/lub aplikacja).
4. **Czas.** Całe zadanie używa `TimeProvider`, a „dziś” liczy w strefie użytkownika. Testy podstawiają `FakeTimeProvider` i przesuwają zegar o dni.

### Treść wiadomości (szablon PL)

```text
Temat: Zostały Ci 2 treningi w pakiecie u Marka Nowaka

Cześć Anna,

w pakiecie „10 treningów” zostały Ci 2 sesje (ważny do 30.11.2026).
Warto porozmawiać z trenerem o odnowieniu, zanim pakiet się skończy.

Zobacz szczegóły: https://app.example.com/packages/3f2a…

Ta wiadomość to przypomnienie z TrainTrack. Zmienisz ustawienia
powiadomień w Profilu.
```

Szablony trzymamy w plikach z miejscami na dane (np. Razor lub prosty silnik szablonów), a w testach sprawdzamy, że każdy ma temat i link.
