# Funkcje (historie użytkowników)

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 4 oraz „Przykład kryterium akceptacji” i „Definicja ukończenia” z rozdziału 11.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.
> Dodano TRN-16 z decyzji BA-03 ([business-rules.md](business-rules.md), 2026-11-15).

## 4. Lista funkcji

Każda funkcja ma identyfikator, który później służy w zadaniach, testach i commitach (np. `feat(PKG-03): licznik sesji`). Priorytety: **P0** = MVP, bez tego aplikacja nie ma sensu; **P1** = ważne, zaraz po MVP; **P2** = miłe uzupełnienie. Kolumna „Etap” odsyła do planu szkolenia z [rozdziału 12](spec-traintrack.md#12-plan-szkolenia-budowa-krok-po-kroku).

### Konto i uwierzytelnianie (AUTH)

| ID | Funkcja | Priorytet | Etap |
| --- | --- | --- | --- |
| AUTH-01 | Rejestracja e-mailem i hasłem (domyślnie rola Client) | P0 | 2 |
| AUTH-02 | Logowanie: odpowiedź z access tokenem (JWT) i refresh tokenem | P0 | 2 |
| AUTH-03 | Odświeżanie sesji refresh tokenem z rotacją | P0 | 3 |
| AUTH-04 | Wykrywanie ponownego użycia starego refresh tokenu i unieważnienie całej rodziny tokenów | P0 | 3 |
| AUTH-05 | Wylogowanie z jednego urządzenia i ze wszystkich | P0 | 3 |
| AUTH-06 | Autoryzacja ról i uprawnień (policy-based) | P0 | 4 |
| AUTH-07 | Blokada konta po serii nieudanych logowań (lockout) | P1 | 8 |
| AUTH-08 | Potwierdzenie adresu e-mail | P1 | 8 |
| AUTH-09 | Reset hasła przez e-mail | P1 | 8 |
| AUTH-10 | Lista aktywnych sesji (urządzeń) z możliwością wylogowania | P1 | 8 |
| AUTH-11 | Dwuskładnikowe uwierzytelnianie (TOTP) | P2 | 9 |
| AUTH-12 | Logowanie przez Google | P2 | 9 |

### Klient (CLI)

| ID | Funkcja | Priorytet | Etap |
| --- | --- | --- | --- |
| CLI-01 | Pulpit: aktywne pakiety z licznikiem „zostało X z Y” i datą ważności | P0 | 5 |
| CLI-02 | Najbliższy zaplanowany trening na pulpicie | P0 | 5 |
| CLI-03 | Szczegóły pakietu: historia sesji i ich statusy | P0 | 5 |
| CLI-04 | Przyjęcie zaproszenia od trenera (kod lub link) | P0 | 5 |
| CLI-05 | Odwołanie zaplanowanej sesji z informacją, czy zużyje ona pakiet | P0 | 6 |
| CLI-06 | Prośba o termin treningu (trener potwierdza) | P1 | 7 |
| CLI-07 | Zakwestionowanie sesji oznaczonej jako odbyta | P1 | 7 |
| CLI-08 | Lista karnetów na siłownię: dodanie, edycja, usunięcie | P0 | 6 |
| CLI-09 | Karnet: pasek „zostało X dni” i kolor statusu (zielony, żółty, czerwony) | P0 | 6 |
| CLI-10 | Karnet: przełącznik „udostępnij trenerowi” | P1 | 7 |
| CLI-11 | Ustawienia przypomnień (progi, kanały) | P1 | 7 |
| CLI-12 | Historia zakończonych pakietów i wydatków | P2 | 9 |
| CLI-13 | Eksport kalendarza treningów (ICS) | P2 | 9 |

### Trener (TRN)

| ID | Funkcja | Priorytet | Etap |
| --- | --- | --- | --- |
| TRN-01 | Zaproszenie klienta e-mailem (jednorazowy kod z terminem ważności) | P0 | 5 |
| TRN-02 | Lista klientów z podsumowaniem: aktywny pakiet, pozostałe sesje, data ważności | P0 | 5 |
| TRN-03 | Filtr „do odnowienia” (mało sesji lub bliska data ważności) | P0 | 5 |
| TRN-04 | CRUD szablonów pakietów (oferta) | P0 | 5 |
| TRN-05 | Przypisanie pakietu klientowi z szablonu, z możliwością zmiany ceny i terminu | P0 | 5 |
| TRN-06 | Zaplanowanie sesji (pojedynczej) dla klienta | P0 | 6 |
| TRN-07 | Oznaczenie sesji: odbyta, nieobecność, odwołana | P0 | 6 |
| TRN-08 | Ręczna korekta liczby sesji z powodem | P0 | 6 |
| TRN-09 | Zapis płatności za pakiet (kwota, data, metoda) i status rozliczenia | P1 | 7 |
| TRN-10 | Kalendarz tygodnia z sesjami wszystkich klientów | P1 | 7 |
| TRN-11 | Rozstrzyganie sporów o sesje | P1 | 7 |
| TRN-12 | Ustawienia: próg odwołania (godz.), przedłużanie ważności | P1 | 7 |
| TRN-13 | Powtarzalne sesje (np. wtorki i czwartki) | P2 | 9 |
| TRN-14 | Raport miesięczny: sesje, przychód, eksport CSV | P2 | 9 |
| TRN-15 | Notatki do sesji (widoczne dla klienta lub prywatne) | P2 | 9 |
| TRN-16 | Przepięcie zaplanowanej sesji do innego pakietu tej samej pary albo zamiana na sesję jednorazową (BA-03) | P1 | 7 |

### Administrator (ADM)

| ID | Funkcja | Priorytet | Etap |
| --- | --- | --- | --- |
| ADM-01 | Lista użytkowników z wyszukiwaniem i filtrem roli | P1 | 8 |
| ADM-02 | Nadawanie i odbieranie ról (np. awans Client → Trainer) | P1 | 8 |
| ADM-03 | Blokada i odblokowanie konta, wymuszone wylogowanie | P1 | 8 |
| ADM-04 | Log audytu: logowania, zmiany ról, korekty pakietów, wglądy admina | P1 | 8 |
| ADM-05 | Podstawowe statystyki systemu (liczba użytkowników, pakietów, przypomnień) | P2 | 9 |

### Systemowe i przekrojowe (SYS)

| ID | Funkcja | Priorytet | Etap |
| --- | --- | --- | --- |
| SYS-01 | Zadanie w tle: codzienne generowanie przypomnień, bez duplikatów | P0 | 6 |
| SYS-02 | Zadanie w tle: wygaszanie przeterminowanych pakietów i karnetów | P0 | 6 |
| SYS-03 | Wysyłka e-mail (SMTP, w środowisku deweloperskim Mailpit) | P0 | 6 |
| SYS-04 | Powiadomienia w aplikacji (dzwonek, licznik nieprzeczytanych) | P1 | 7 |
| SYS-05 | Ujednolicony format błędów (ProblemDetails, RFC 9457) i walidacja | P0 | 2 |
| SYS-06 | Paginacja, sortowanie i filtrowanie list | P0 | 5 |
| SYS-07 | Dokumentacja API (OpenAPI) i wygenerowany klient TypeScript | P0 | 4 |
| SYS-08 | Logowanie strukturalne (Serilog) z identyfikatorem żądania | P1 | 8 |
| SYS-09 | Ograniczanie liczby żądań (rate limiting) na logowaniu i odświeżaniu | P1 | 8 |
| SYS-10 | Web push | P2 | 9 |
| SYS-11 | Wersja językowa PL i EN (i18n) | P2 | 9 |
| SYS-12 | Tryb ciemny i pełna obsługa telefonu (mobile first) | P1 | 7 |

## 11. Kryteria akceptacji i definicja ukończenia (fragment rozdziału 11)

### Przykład kryterium akceptacji (Gherkin)

Każda funkcja z [rozdziału 4](#4-lista-funkcji) dostaje kryteria w takiej postaci; można je potem zamienić na testy.

```gherkin
Funkcja: Odwołanie sesji przez klienta (CLI-05)

  Scenariusz: Późne odwołanie zużywa sesję z pakietu
    Zakładając, że pakiet klienta ma 5 pozostałych sesji
    I trener ustawił próg odwołania na 24 godziny
    I sesja jest zaplanowana za 12 godzin
    Gdy klient odwołuje sesję
    Wtedy status sesji to "CancelledByClient"
    I sesja zużywa pakiet
    I pozostało 4 sesje

  Scenariusz: Wczesne odwołanie nie zużywa sesji
    Zakładając, że sesja jest zaplanowana za 30 godzin
    Gdy klient odwołuje sesję
    Wtedy sesja nie zużywa pakietu
    I pozostało 5 sesji
```

### Definicja ukończenia (Definition of Done)

Funkcja jest skończona dopiero, gdy:

- [ ] logika domeny ma testy jednostkowe, w tym przypadki brzegowe,
- [ ] endpoint ma test integracyjny dla każdej roli (`200` / `403` / `404`) i dla błędów domeny (`409`),
- [ ] walidacja po obu stronach daje czytelne komunikaty po polsku,
- [ ] ekran obsługuje stany ładowania, pusty i błędu, a skan axe jest zielony,
- [ ] OpenAPI i wygenerowany klient są zaktualizowane, migracja bazy jest w repozytorium,
- [ ] akcje wrażliwe zapisują wpis w logu audytu,
- [ ] potok CI jest zielony, a pull request ma opis z identyfikatorem funkcji.
