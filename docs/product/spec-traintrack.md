# Specyfikacja: aplikacja do śledzenia treningów i karnetów

Oct 21, 2026 · @Krzysztof

## 1. Cel, kontekst i zakres

**TrainTrack** to aplikacja internetowa, która pilnuje dwóch rzeczy: ile treningów zostało w pakiecie u trenera personalnego oraz kiedy kończy się karnet na siłownię. Klient i trener widzą ten sam, jeden stan pakietu, a system przypomina o odnowieniu, zanim pakiet lub karnet wygaśnie.

Dokument ma dwa zadania: jest specyfikacją produktu oraz punktem wyjścia do praktycznego szkolenia z budowy aplikacji fullstack (.NET + React). Dlatego każdy rozdział kończy się czymś, co można zbudować i przetestować, a rozdział 12 układa to w kolejne etapy.

### Problem, który rozwiązujemy

- Klient nie wie dokładnie, ile treningów mu zostało i do kiedy ważny jest pakiet.
- Trener liczy sesje w zeszycie lub arkuszu i musi sam pamiętać o rozmowie o odnowieniu.
- Karnet na siłownię wygasa niezauważony, bo nikt nie przypomina o jego końcu.
- Po obu stronach pojawiają się spory typu "to był ten trening czy nie", bo brakuje wspólnej historii.

### Zakres

| Obszar | MVP (etapy 1–6) | Później (etapy 7+) |
| --- | --- | --- |
| Konta i dostęp | Rejestracja, logowanie, JWT + refresh token, role Klient / Trener / Admin | Logowanie przez Google, 2FA, reset hasła przez SMS |
| Pakiety | Oferta trenera, przypisanie pakietu klientowi, licznik sesji, ważność, status | Wiele cen i promocji, pakiety grupowe, zamrożenie pakietu |
| Treningi | Zaplanowanie, oznaczenie jako odbyty / odwołany / nieobecność, historia | Kalendarz z widokiem tygodnia, powtarzalne sesje, eksport ICS |
| Karnety | Karnet prywatny klienta, daty, przypomnienia | Karnety wejściowe z licznikiem wejść, udostępnienie trenerowi |
| Przypomnienia | E-mail oraz powiadomienia w aplikacji, zadanie w tle raz dziennie | Web push, SMS, własne progi przypomnień |
| Rozliczenia | Ręczny zapis płatności za pakiet | Raport miesięczny trenera (CSV), faktury |
| Poza zakresem | Bramka płatności online, aplikacja mobilna natywna, plany treningowe i ćwiczenia |  |

### Kryteria sukcesu

- Klient po zalogowaniu w 3 sekundy widzi: liczbę pozostałych treningów, datę ważności i najbliższy trening.
- Trener po zalogowaniu widzi listę klientów, którzy wymagają odnowienia pakietu.
- Żaden pakiet ani karnet nie wygasa bez przynajmniej jednego przypomnienia wysłanego z wyprzedzeniem.
- Licznik pozostałych sesji zawsze zgadza się z historią treningów (wymusza to model danych, nie tylko interfejs).

## 2. Role, uprawnienia i macierz RBAC

Dostęp opiera się na trzech rolach, a każda rola to zestaw nazwanych uprawnień (permissions). Sama rola nie wystarcza: trener widzi dane **tylko swoich** klientów, a klient **tylko swoje**. Dlatego każdy endpoint sprawdza dwie rzeczy: czy rola ma uprawnienie oraz czy użytkownik jest właścicielem zasobu (autoryzacja oparta na zasobie).

### Role

| Rola | Kim jest | Zasada ogólna |
| --- | --- | --- |
| Client | Osoba trenująca | Widzi i zmienia wyłącznie własne dane, pakiety i karnety |
| Trainer | Trener personalny | Zarządza swoją ofertą, klientami, pakietami i sesjami; nie widzi karnetów klienta bez jego zgody |
| Admin | Właściciel systemu | Zarządza kontami i rolami, nie wchodzi w treningi (wgląd tylko do odczytu, każdy wgląd trafia do logu audytu) |

Jeden użytkownik może mieć kilka ról (trener, który sam też trenuje u innego trenera, ma Trainer + Client). Token zawiera wszystkie jego role, a interfejs pozwala przełączać widok.

### Macierz uprawnień

Legenda: **W** = własne zasoby, **K** = zasoby swoich klientów, **T** = wszystkie, **–** = brak dostępu.

| Zasób / akcja | Client | Trainer | Admin |
| --- | --- | --- | --- |
| Własny profil: odczyt i edycja | W | W | W |
| Zaproszenie klienta (e-mail + kod) | – | W | – |
| Przyjęcie zaproszenia | W | – | – |
| Lista klientów trenera | – | K | T (odczyt) |
| Szablon pakietu (oferta): CRUD | – | W | – |
| Pakiet: podgląd | W | K | T (odczyt) |
| Pakiet: utworzenie dla klienta | – | K | – |
| Pakiet: korekta ręczna liczby sesji (z powodem) | – | K | – |
| Pakiet: anulowanie | – | K | – |
| Sesja: podgląd | W | K | T (odczyt) |
| Sesja: zaplanowanie | prośba (W) | K | – |
| Sesja: oznaczenie odbyta / nieobecność | – | K | – |
| Sesja: odwołanie | W | K | – |
| Sesja: zakwestionowanie odbycia | W | – | – |
| Płatność za pakiet: zapis | – | K | – |
| Karnet na siłownię: CRUD | W | – | – |
| Karnet: podgląd przez trenera (po zgodzie klienta) | – | K, tylko jeśli udostępniony | – |
| Ustawienia przypomnień | W | W | – |
| Sesje logowania (urządzenia): podgląd i wylogowanie | W | W | T (wymuszone wylogowanie) |
| Użytkownicy: lista, blokada, zmiana ról | – | – | T |
| Log audytu | – | – | T |

### Jak to zapisać w kodzie

- Uprawnienia mają postać stałych tekstowych, np. `packages.read`, `packages.create`, `sessions.complete`, `memberships.manage`, `users.manage`.
- Mapowanie rola → uprawnienia siedzi w jednym miejscu (klasa `Permissions` + seed w bazie), a w JWT trafiają tylko role.
- Uprawnienia sprawdza **policy-based authorization** w ASP.NET Core: `[Authorize(Policy = "packages.create")]`.
- Własność zasobu sprawdza osobny `IAuthorizationHandler` (np. `TrainerOwnsClientRequirement`), nie logika rozsiana po kontrolerach.
- Gdy użytkownik zna identyfikator cudzego zasobu, API zwraca `404`, a nie `403`, żeby nie ujawniać, że zasób istnieje.
- Po stronie React role służą tylko do ukrywania nawigacji i przycisków. Prawdziwa ochrona jest zawsze w API.

## 3. Reguły biznesowe i słownik pojęć

Ta część to serce domeny. Najwięcej błędów w takich aplikacjach bierze się z niejasnych reguł (czy odwołanie w dniu treningu zużywa sesję?), więc zapisujemy je jawnie i każdą pokrywamy testem jednostkowym.

### Słownik

| Pojęcie | Znaczenie |
| --- | --- |
| Szablon pakietu | Oferta trenera: nazwa, liczba sesji, cena, ważność w dniach, czas trwania sesji |
| Pakiet | Konkretny zakup klienta u trenera, utworzony z szablonu (kopia warunków z chwili zakupu) |
| Sesja | Pojedynczy trening klienta z trenerem, przypięty do pakietu albo jednorazowy |
| Zużyta sesja | Sesja, która pomniejsza licznik pakietu |
| Karnet | Prywatny karnet klienta na siłownię (nazwa klubu, daty, cena) |
| Przypomnienie | Powiadomienie o kończącym się pakiecie lub karnecie |
| Para trener–klient | Relacja powstająca po przyjęciu zaproszenia; bez niej trener nie widzi klienta |

### Statusy pakietu

| Status | Kiedy | Można planować sesje |
| --- | --- | --- |
| `PendingPayment` | Utworzony, jeszcze bez wpłaty (opcjonalny, ustawienie trenera) | Tak, jeśli trener tak ustawi |
| `Active` | Są wolne sesje i pakiet jest w terminie | Tak |
| `Completed` | Zużyto wszystkie sesje | Nie |
| `Expired` | Minęła data ważności, zostały sesje | Nie (trener może przedłużyć) |
| `Cancelled` | Anulowany przez trenera | Nie |

### Statusy sesji

`Planned` → `Completed` | `CancelledByClient` | `CancelledByTrainer` | `NoShow`. Dodatkowo `Disputed`, gdy klient zakwestionował oznaczenie sesji jako odbytej.

### Zasady zużywania sesji

| Zdarzenie | Zużywa sesję z pakietu? |
| --- | --- |
| Trener oznacza sesję jako odbytą | Tak |
| Klient nie przyszedł (`NoShow`) | Tak |
| Klient odwołuje najpóźniej 24 h przed startem | Nie |
| Klient odwołuje później niż 24 h przed startem | Tak (próg 24 h to ustawienie trenera, 0–72 h) |
| Trener odwołuje sesję | Nie, zawsze |
| Klient kwestionuje odbytą sesję | Sesja jest wstrzymana (`Disputed`) do decyzji trenera, licznik jest przywracany do czasu rozstrzygnięcia |

### Reguły pakietu

1. Liczba pozostałych sesji = `TotalSessions` − liczba sesji ze statusem zużywającym. **Nie przechowujemy** tej liczby w osobnej kolumnie, tylko liczymy ją (lub trzymamy jako kolumnę obliczaną), żeby nigdy nie rozjechała się z historią.
2. Nie można zaplanować więcej sesji niż pozostało wolnych (planowane sesje też rezerwują miejsce).
3. Data ważności = data pierwszej sesji + `ValidityDays` (lub data zakupu, zależnie od ustawienia szablonu). Sesji nie da się zaplanować po dacie ważności.
4. Pakiet przechodzi w `Completed` automatycznie, gdy ostatnia sesja zostaje rozliczona, a w `Expired` przez zadanie w tle po dacie ważności.
5. Korekta ręczna (np. bonusowa sesja) zawsze wymaga powodu i trafia do logu audytu z autorem i datą.
6. Zmiana szablonu **nie zmienia** już sprzedanych pakietów, bo pakiet kopiuje warunki w chwili zakupu.
7. Klient może mieć naraz wiele aktywnych pakietów u różnych trenerów; sesja jest zawsze przypięta do konkretnego pakietu (domyślnie najstarszego aktywnego).

### Reguły karnetu

- Karnet jest własnością klienta. Trener widzi go tylko wtedy, gdy klient włączy udostępnianie przy tym karnecie.
- Karnet ma datę początku i końca. Przy karnetach wejściowych (później) dochodzi licznik wejść.
- Klient ustawia własne progi przypomnień (domyślnie 14, 7 i 1 dzień przed końcem).

### Reguły przypomnień

| Powód | Domyślny próg | Do kogo |
| --- | --- | --- |
| Pakiet: zostały ≤ 2 wolne sesje | 2 sesje | Klient i trener |
| Pakiet: zbliża się data ważności | 14 i 7 dni | Klient i trener |
| Pakiet: wygasł z niewykorzystanymi sesjami | w dniu wygaśnięcia | Klient i trener |
| Karnet: zbliża się koniec | 14, 7, 1 dzień | Klient (oraz trener, jeśli karnet udostępniony) |
| Zaplanowana sesja jutro | 24 h przed | Klient i trener |

Ta sama kombinacja (zdarzenie + próg + odbiorca) nigdy nie wysyła się dwa razy. To wymaga zapisu wysłanych przypomnień w bazie (rozdział 10).

## 4. Lista funkcji

Każda funkcja ma identyfikator, który później służy w zadaniach, testach i commitach (np. `feat(PKG-03): licznik sesji`). Priorytety: **P0** = MVP, bez tego aplikacja nie ma sensu; **P1** = ważne, zaraz po MVP; **P2** = miłe uzupełnienie. Kolumna „Etap” odsyła do planu szkolenia z rozdziału 12.

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

## 5. Ekrany

Poniżej 17 szkiców: 16 w układzie desktopowym (z dwoma oknami dialogowymi) i jeden w układzie telefonu. To wireframe'y, czyli szkice układu i treści, a nie projekt graficzny. Zasady: najważniejsza liczba (ile zostało sesji, ile dni do końca karnetu) jest zawsze największa i widoczna bez przewijania; kolor i znak `(!)` ostrzegają, gdy zostały ≤ 2 sesje lub ≤ 14 dni; każda akcja, która zmienia licznik, mówi wprost, czy zużyje sesję. Interfejs ma być mobile first, bo klient zagląda do aplikacji głównie z telefonu.

Legenda szkiców: `[ tekst ]` pole formularza, `( Tekst )` przycisk, `[x]` / `[ ]` pole wyboru, `(•)` / `( )` przełącznik, `[Nazwa]` aktywna zakładka, `[!3]` dzwonek z trzema nieprzeczytanymi, `(!)` ostrzeżenie.

### Inwentarz ekranów

| ID | Ekran | Ścieżka | Rola | Funkcje |
| --- | --- | --- | --- | --- |
| S-01 | Logowanie | `/login` | publiczny | AUTH-02 |
| S-02 | Rejestracja (także z kodu zaproszenia) | `/register` | publiczny | AUTH-01, CLI-04 |
| S-03 | Profil i aktywne sesje | `/profile` | zalogowany | AUTH-05, AUTH-10 |
| K-01 | Pulpit klienta | `/` | Client | CLI-01, CLI-02, CLI-09 |
| K-02 | Szczegóły pakietu | `/packages/:id` | Client | CLI-03, CLI-05, CLI-07 |
| K-03 | Lista karnetów (+ dialog nowego karnetu) | `/memberships` | Client | CLI-08, CLI-09, CLI-10 |
| K-04 | Ustawienia przypomnień | `/settings/notifications` | zalogowany | CLI-11 |
| K-05 | Pulpit klienta na telefonie | `/` | Client | CLI-01, CLI-02, SYS-12 |
| T-01 | Pulpit trenera | `/` | Trainer | TRN-02, TRN-03, TRN-07 |
| T-02 | Lista klientów | `/clients` | Trainer | TRN-01, TRN-02, TRN-03 |
| T-03 | Karta klienta | `/clients/:id` | Trainer | TRN-05, TRN-08, TRN-09 |
| T-04 | Dialog: nowy pakiet | w T-03 / T-01 | Trainer | TRN-05 |
| T-05 | Kalendarz tygodnia | `/calendar` | Trainer | TRN-06, TRN-10 |
| T-06 | Oferta (szablony pakietów) | `/offer` | Trainer | TRN-04, TRN-12 |
| A-01 | Użytkownicy i role | `/admin/users` | Admin | ADM-01, ADM-02, ADM-03 |
| A-02 | Log audytu | `/admin/audit` | Admin | ADM-04 |

### Konto: logowanie, rejestracja, profil

**S-01 Logowanie**

```text
┌────────────────────────────────────────────────┐
│                                                │
│                   TrainTrack                   │
│                                                │
│ Zaloguj się                                    │
│                                                │
│ E-mail                                         │
│ [ anna@example.com                         ]   │
│                                                │
│ Hasło                                          │
│ [ ********                           [o]   ]   │
│                                                │
│ ( Zaloguj się )     Nie pamiętasz hasła?       │
│                                                │
│ Nie masz konta? Zarejestruj się                │
│                                                │
└────────────────────────────────────────────────┘
```

Błędne dane dają jeden, ogólny komunikat („Nieprawidłowy e-mail lub hasło”), żeby nie ujawniać, które konta istnieją.

**S-02 Rejestracja** (pole „Zaproszenie od” pojawia się tylko, gdy wejście było z linku zaproszenia)

```text
┌────────────────────────────────────────────────┐
│                                                │
│                  Załóż konto                   │
│                                                │
│ Zaproszenie od: Marek Nowak (trener)           │
│                                                │
│ Imię i nazwisko                                │
│ [ Anna Kowalska                            ]   │
│ E-mail                                         │
│ [ anna@example.com                         ]   │
│ Hasło (min. 10 znaków)                         │
│ [ **********                               ]   │
│ Powtórz hasło                                  │
│ [ **********                               ]   │
│                                                │
│ [x] Akceptuję regulamin i politykę prywatności │
│                                                │
│ ( Utwórz konto )                               │
│ Masz już konto? Zaloguj się                    │
│                                                │
└────────────────────────────────────────────────┘
```

**S-03 Profil i aktywne sesje**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit    Pakiety    Karnety    Kalendarz   [!3] Anna ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Profil                                                               │
│ Imię i nazwisko [ Anna Kowalska              ]                       │
│ E-mail           anna@example.com (potwierdzony)                     │
│                                                                      │
│ Zmiana hasła  ( Zmień hasło )                                        │
│                                                                      │
│ AKTYWNE SESJE (URZĄDZENIA)                                           │
│ Urządzenie                Ostatnia aktywność      Akcja              │
│ Chrome · Windows          teraz (to urządzenie)   —                  │
│ Safari · iPhone           wczoraj, 19:42          ( Wyloguj )        │
│                                                                      │
│ ( Wyloguj ze wszystkich urządzeń )                                   │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

Lista urządzeń to widok tabeli refresh tokenów (rozdział 7): każdy wiersz to jedna rodzina tokenów.

### Klient

**K-01 Pulpit klienta**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack   [Pulpit]   Pakiety    Karnety    Kalendarz   [!3] Anna ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Cześć, Anna                                                          │
│                                                                      │
│ MOJE PAKIETY                                                         │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Pakiet 10 treningów · trener Marek Nowak                         │ │
│ │                                                                  │ │
│ │ ████████████████████████░░░░░░  Zostało 2 z 10                   │ │
│ │ Ważny do 30.11.2026 (jeszcze 40 dni)                             │ │
│ │ (!) Zostały tylko 2 sesje – porozmawiaj z trenerem o odnowieniu  │ │
│ │                                                                  │ │
│ │ ( Szczegóły pakietu )                                            │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ NAJBLIŻSZY TRENING                                                   │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Czwartek 22.10.2026, 18:00 (60 min) · Marek Nowak                │ │
│ │ ( Odwołaj )   Odwołanie do 21.10, 18:00 nie zużywa sesji         │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ KARNET NA SIŁOWNIĘ                                                   │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ CityFit Premium · ważny do 02.11.2026                            │ │
│ │                                                                  │ │
│ │ ██████████████████░░░░░░░░░░░░  zostało 12 dni (!)               │ │
│ │ ( Zarządzaj karnetami )                                          │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**K-05 Ten sam pulpit na telefonie** (karty jedna pod drugą, nawigacja na dole)

```text
┌────────────────────────────────────┐
│ ≡  TrainTrack                 [!3] │
├────────────────────────────────────┤
│ Cześć, Anna                        │
│                                    │
│ PAKIET                             │
│ ┌────────────────────────────────┐ │
│ │ Pakiet 10 treningów            │ │
│ │ Marek Nowak                    │ │
│ │ ████████████████░░░░           │ │
│ │ Zostało 2 z 10                 │ │
│ │ Ważny do 30.11.2026            │ │
│ │ (!) czas na odnowienie         │ │
│ │ ( Szczegóły )                  │ │
│ └────────────────────────────────┘ │
│                                    │
│ NAJBLIŻSZY TRENING                 │
│ ┌────────────────────────────────┐ │
│ │ Czw 22.10, 18:00               │ │
│ │ Marek Nowak                    │ │
│ │ ( Odwołaj )                    │ │
│ └────────────────────────────────┘ │
│                                    │
│ KARNET                             │
│ ┌────────────────────────────────┐ │
│ │ CityFit Premium                │ │
│ │ zostało 12 dni (!)             │ │
│ │ ████████████░░░░░░░░           │ │
│ └────────────────────────────────┘ │
│                                    │
├────────────────────────────────────┤
│ Pulpit  Pakiety  Karnety  Więcej   │
└────────────────────────────────────┘
```

**K-02 Szczegóły pakietu** (kolumna „Zużywa” wynika z reguł z rozdziału 3, klient widzi, dlaczego licznik się zmienił)

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit   [Pakiety]   Karnety    Kalendarz   [!3] Anna ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ < Pakiety                                                            │
│                                                                      │
│ Pakiet 10 treningów · Marek Nowak              Status: Aktywny       │
│ Zakup 02.09.2026 · ważny do 30.11.2026 · 1 200 zł (opłacony)         │
│                                                                      │
│ ████████████████████████░░░░░░  Zostało 2 z 10 (1 zaplanowana)       │
│                                                                      │
│ HISTORIA SESJI                                                       │
│ Nr    Data              Status                          Zużywa       │
│ 1     03.09.2026 18:00  Odbyty                          tak          │
│ 2     08.09.2026 18:00  Odbyty                          tak          │
│ 3     10.09.2026 18:00  Odbyty                          tak          │
│ –     15.09.2026 18:00  Odwołany przez klienta (>24 h)  nie          │
│ 4     17.09.2026 18:00  Odbyty                          tak          │
│ 5     24.09.2026 18:00  Odbyty                          tak          │
│ 6     01.10.2026 18:00  Nieobecność                     tak          │
│ 7     08.10.2026 18:00  Odbyty                          tak          │
│ 8     15.10.2026 18:00  Odbyty                          tak          │
│ 9     22.10.2026 18:00  Zaplanowany                     rezerwuje    │
│                                                                      │
│ ( Odwołaj najbliższą sesję )   ( Zakwestionuj sesję )                │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**K-03 Lista karnetów** (karnet należy do klienta; przełącznik „Udostępniony trenerowi” decyduje, czy trener go zobaczy)

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit    Pakiety   [Karnety]   Kalendarz   [!3] Anna ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Moje karnety                                      ( + Dodaj karnet ) │
│                                                                      │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ CityFit Premium · czasowy · 99 zł/mies.                          │ │
│ │ 01.10.2026 – 02.11.2026 · zostało 12 dni (!)                     │ │
│ │ [x] Udostępniony trenerowi    ( Edytuj ) ( Usuń )                │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Basen Zdrowie · wejściowy (10 wejść)                             │ │
│ │ do 31.12.2026 · wykorzystano 4 z 10 wejść                        │ │
│ │ [ ] Udostępniony trenerowi    ( Edytuj ) ( Usuń )                │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**K-03 (dialog) Nowy karnet**

```text
┌──────────────────────────────────────────────────────────────────┐
│ Nowy karnet                                                      │
│                                                                  │
│ Nazwa klubu                                                      │
│ [ CityFit Premium                                ]               │
│ Typ            (•) czasowy   ( ) wejściowy                       │
│ Od [ 01.10.2026   ]   Do [ 02.11.2026   ]                        │
│ Cena [ 99,00 zł     ]                                            │
│                                                                  │
│ [x] Udostępnij ten karnet trenerowi                              │
│ Przypomnij:  [x] 14 dni  [x] 7 dni  [x] 1 dzień przed końcem     │
│                                                                  │
│ ( Anuluj )  ( Zapisz )                                           │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**K-04 Ustawienia przypomnień**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit    Pakiety    Karnety    Kalendarz   [!3] Anna ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Powiadomienia i przypomnienia                                        │
│                                                                      │
│ Zdarzenie                               E-mail      W aplikacji      │
│ Pakiet: zostały ≤ 2 sesje               [x]         [x]              │
│ Pakiet: zbliża się ważność (14 i 7 dni) [x]         [x]              │
│ Pakiet: wygasł                          [x]         [x]              │
│ Karnet: zbliża się koniec               [x]         [x]              │
│ Trening jutro                           [x]         [ ]              │
│                                                                      │
│ Progi karnetu (dni przed końcem): [ 14, 7, 1   ]                     │
│                                                                      │
│ ( Zapisz )                                                           │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Trener

**T-01 Pulpit trenera** (lista „Do odnowienia” to najważniejszy element: tu trener widzi, z kim rozmawiać o nowym pakiecie)

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack   [Pulpit]   Klienci    Kalendarz    Oferta   [!1] Marek ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Dzień dobry, Marek                                                   │
│                                                                      │
│ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │
│ │ Klienci      │ │ Do odnowienia│ │ Sesje dziś   │ │ Sesje w tyg. │  │
│ │ 12           │ │ 3            │ │ 4            │ │ 18           │  │
│ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘  │
│                                                                      │
│ DO ODNOWIENIA                                                        │
│ Klient              Zostało     Ważny do                             │
│ Anna Kowalska       2 z 10      30.11.2026      ( Nowy pakiet )      │
│ Piotr Zieliński     1 z 20      15.11.2026      ( Nowy pakiet )      │
│ Ola Maj             5 z 10      03.11.2026 (!)  ( Przedłuż )         │
│                                                                      │
│ DZISIEJSZE TRENINGI                                                  │
│ 16:00   Piotr Zieliński     Odbyty                                   │
│ 18:00   Anna Kowalska       Zaplanowany   ( Odbył się ) ( Nieobecny )│
│ 19:00   Ola Maj             Zaplanowany   ( Odbył się ) ( Nieobecny )│
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**T-02 Lista klientów**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit   [Klienci]   Kalendarz    Oferta   [!1] Marek ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Klienci                                      ( + Zaproś klienta )    │
│                                                                      │
│ [ Szukaj klienta…          ]  (Wszyscy) (Do odnowienia) (Bez pakietu)│
│                                                                      │
│ Klient            Pakiet        Zostało   Ważny do    Status         │
│ Anna Kowalska     10 treningów  2 z 10    30.11.2026  Do odnowienia  │
│ Piotr Zieliński   20 treningów  1 z 20    15.11.2026  Do odnowienia  │
│ Ola Maj           10 treningów  5 z 10    03.11.2026  Aktywny        │
│ Tomasz Lis        —             —         —           Brak pakietu   │
│ Ewa Nowicka       10 treningów  0 z 10    —           Wygasł         │
│                                                                      │
│                                                   < 1  2  3 >        │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**T-03 Karta klienta** (zakładka „Karnet” pojawia się tylko, gdy klient udostępnił karnet; inaczej widać komunikat „Karnet klienta: nie udostępniony”)

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit   [Klienci]   Kalendarz    Oferta   [!1] Marek ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ < Klienci                                                            │
│ Anna Kowalska · anna@example.com · klientka od 02.09.2026            │
│                                                                      │
│ [Pakiety]  Sesje  Płatności  Karnet                                  │
│                                                                      │
│ AKTYWNY PAKIET                                                       │
│ ┌──────────────────────────────────────────────────────────────────┐ │
│ │ Pakiet 10 treningów · Zostało 2 z 10 · ważny do 30.11.2026       │ │
│ │ ████████████████████████░░░░░░                                   │ │
│ │ ( Korekta sesji ) ( Przedłuż ważność ) ( Anuluj pakiet )         │ │
│ └──────────────────────────────────────────────────────────────────┘ │
│                                                                      │
│ POPRZEDNIE PAKIETY                                                   │
│ Pakiet 10 treningów · 02.03.2026 – 28.08.2026 · zakończony (10/10)   │
│                                                                      │
│ PŁATNOŚCI                                                            │
│ Data          Kwota       Metoda      Pakiet                         │
│ 02.09.2026    1 200 zł    przelew     10 treningów                   │
│ ( + Dodaj płatność )                                                 │
│                                                                      │
│ Karnet klienta: nie udostępniony                                     │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**T-04 Dialog: nowy pakiet** (po wyborze szablonu pola wypełniają się same, ale trener może je zmienić dla tego klienta)

```text
┌────────────────────────────────────────────────────────────────┐
│ Nowy pakiet                                                    │
│                                                                │
│ Klient            [ Anna Kowalska ▾              ]             │
│ Szablon           [ 10 treningów ▾               ]             │
│ Liczba sesji      [ 10       ]   Cena  [ 1 200 zł   ]          │
│ Ważność (dni)     [ 90       ]                                 │
│ Liczyć ważność    (•) od zakupu   ( ) od pierwszej sesji       │
│ Płatność          (•) opłacony   ( ) oczekuje                  │
│                                                                │
│ ( Anuluj )  ( Utwórz pakiet )                                  │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

**T-05 Kalendarz tygodnia**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit    Klienci   [Kalendarz]   Oferta   [!1] Marek ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ < Poprzedni   19 – 23 października 2026   Następny >   ( + Sesja )   │
│                                                                      │
│        Pon 19      Wt 20       Śr 21       Czw 22      Pt 23         │
│ 16:00              Piotr Z. OK Piotr Z. OK             Ola M.        │
│ 17:00  Anna K. OK                                                    │
│ 18:00              Anna K. OK  Anna K.     Anna K.     Tomek L.      │
│ 19:00  Ola M. OK               Ola M.      Ola M.                    │
│                                                                      │
│ OK = odbyty    bez znaku = zaplanowany                               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**T-06 Oferta (szablony pakietów)**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Pulpit    Klienci    Kalendarz   [Oferta]  [!1] Marek ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Moje pakiety (szablony)                     ( + Nowy szablon )       │
│                                                                      │
│ Nazwa                 Sesje    Cena        Ważność     Akcje         │
│ Próbny                1        80 zł       14 dni      ( Edytuj )    │
│ 10 treningów          10       1 200 zł    90 dni      ( Edytuj )    │
│ 20 treningów          20       2 200 zł    180 dni     ( Edytuj )    │
│                                                                      │
│ Próg bezkosztowego odwołania: [ 24 ] godz.                           │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Administrator

**A-01 Użytkownicy i role**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack   [Użytkownicy]   Audyt                            Admin ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Użytkownicy                                                          │
│ [ Szukaj po e-mailu…       ]  Rola: (Wszystkie ▾)                    │
│                                                                      │
│ E-mail              Role             Status       Ostatnie logowanie │
│ anna@example.com    Client           Aktywny      dziś 16:02         │
│ marek@example.com   Trainer, Client  Aktywny      dziś 15:40         │
│ ola@example.com     Client           Zablokowany  12.10.2026         │
│                                                                      │
│ Wybrany: marek@example.com                                           │
│ Role: [x] Client  [x] Trainer  [ ] Admin    ( Zapisz role )          │
│ ( Zablokuj konto )  ( Wymuś wylogowanie )                            │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**A-02 Log audytu**

```text
┌──────────────────────────────────────────────────────────────────────┐
│ TrainTrack    Użytkownicy   [Audyt]                           Admin ▾│
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│ Log audytu                       Typ: (Wszystkie ▾)  Od–Do: [ … ]    │
│                                                                      │
│ Czas              Kto                Akcja           Obiekt          │
│ 21.10.2026 16:02  anna@example.com   Logowanie       —               │
│ 21.10.2026 15:12  marek@example.com  Korekta (+1)    Pakiet #42      │
│ 20.10.2026 09:30  admin@example.com  Zmiana ról      ola@example.com │
│ 19.10.2026 21:05  admin@example.com  Wgląd w pakiet  Pakiet #17      │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### Stany, które każdy ekran musi obsłużyć

To lista kontrolna do wdrożenia i do testów (testerski nawyk przyda się tu od pierwszego dnia).

- **Ładowanie:** szkielet (skeleton) zamiast pustej strony.
- **Pusto:** klient bez pakietu widzi „Poproś trenera o pakiet lub wpisz kod zaproszenia”; trener bez klientów widzi przycisk „Zaproś pierwszego klienta”.
- **Błąd sieci lub serwera:** komunikat z przyciskiem „Spróbuj ponownie”, bez utraty wpisanych danych.
- **Wygaśnięcie sesji:** po nieudanym odświeżeniu tokenu przekierowanie na `/login` z powrotem na tę samą stronę po zalogowaniu.
- **Brak uprawnień:** ekran `403`, a dla cudzego zasobu ekran „Nie znaleziono” (zgodnie z regułą 404 z rozdziału 2).
- **Kolory nie niosą znaczenia samodzielnie:** zawsze towarzyszy im tekst lub ikona (dostępność).
- **Konflikt:** gdy ktoś zmienił dane w międzyczasie, interfejs pokazuje komunikat o konflikcie i odświeża widok (patrz kontrola współbieżności, rozdział 6).

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
| `RefreshToken` | `Id`, `UserId`, `FamilyId`, `TokenHash`, `ExpiresAt`, `UsedAt`, `RevokedAt`, `ReplacedByTokenId`, `UserAgent`, `CreatedByIp` | W bazie tylko skrót SHA-256 tokenu; jedna rodzina = jedno urządzenie (rozdział 7) |
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

Dochodzi jeszcze kolejka `OutboxMessage` (`Type`, `PayloadJson`, `CreatedAt`, `ProcessedAt`, `Attempts`, `Error`), w której czekają e-maile do wysłania (rozdział 10).

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

- **Backend (.NET):** `AddAuthentication().AddJwtBearer(...)` z `MapInboundClaims = false`, żeby claimy `sub` i `role` zachowały swoje nazwy. Domyślna polityka wymaga zalogowanego użytkownika (`FallbackPolicy`), a endpointy `auth` oznaczamy `[AllowAnonymous]`. Uprawnienia z rozdziału 2 stają się politykami (`AddAuthorization`), a własność zasobu sprawdza `IAuthorizationHandler`.
- **Frontend (React):** kontekst `AuthProvider` trzyma access token w pamięci i dane użytkownika; hak `useAuth()` udostępnia role; komponent `<RequireRole roles={["Trainer"]}>` chroni trasy i ukrywa elementy interfejsu. Ochrona w React jest tylko wygodą, a bezpieczeństwo zapewnia wyłącznie API.
- **Testy:** dla każdej pary (endpoint, rola) powstaje test integracyjny sprawdzający `200` / `403` / `404` oraz test, że trener A nie widzi klienta trenera B.

## 8. Projekt REST API

API jest zasobowe (REST) i zwraca JSON. Każdy endpoint, poza `/api/auth/register`, `/api/auth/login` i `/api/auth/refresh`, wymaga access tokenu. Kolumna „Kto” podaje role z uprawnieniem; zakres danych (własne lub swoich klientów) wynika z rozdziału 2.

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

## 9. Architektura i technologie

Aplikacja to **modularny monolit**: jeden backend .NET wystawiający REST API, jedna aplikacja jednostronicowa (SPA) w React i jedna baza PostgreSQL. Mikroserwisy byłyby tu przerostem formy, a monolit w czystych warstwach uczy tego, co przydaje się najczęściej w pracy: podziału odpowiedzialności, wstrzykiwania zależności i testowalności.

Diagram pokazuje kierunek zależności między projektami backendu.

&#91;embedded content: architektura backendu · 4 projekty, 2 usługi zewnętrzne\]

Api i Worker wywołują przypadki użycia z Application, Application korzysta z Domain, a Infrastructure implementuje interfejsy z Application i dopiero ona rozmawia z PostgreSQL i serwerem poczty. Dzięki temu reguły z rozdziału 3 żyją w Domain i testuje się je bez bazy.

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

- **Domain:** encje mają zachowanie, a nie tylko pola (`package.ScheduleSession(...)`, `session.Complete(...)`). Reguły z rozdziału 3 to małe, czyste metody, które testuje się bez bazy i bez frameworka.
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
- Każdy ekran z rozdziału 5 obsługuje stany: ładowanie, pusty, błąd.

### Środowisko lokalne i CI

1. `docker compose up` uruchamia PostgreSQL i Mailpit (podgląd wysłanych e-maili); API i Vite uruchamiamy z IDE lub terminala.
2. Dane demonstracyjne (administrator, trener Marek, klientka Anna, przykładowe pakiety) wgrywa seeder wyłącznie w środowisku `Development`.
3. Potok CI przy każdym pull requeście: budowanie, testy jednostkowe i integracyjne (Testcontainers), lint i testy frontendu, a na końcu Playwright na uruchomionym stosie.
4. Praca na krótkich gałęziach z pull requestami, nawet gdy repozytorium jest tylko Twoje: nawyk ten procentuje w zespole. Commity opisują funkcję identyfikatorem z rozdziału 4.

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

Do każdego klucza dochodzi kanał (e-mail lub aplikacja). Odbiorcy wynikają z rozdziału 3: pakiet dotyczy klienta i trenera, karnet klienta (a trenera tylko wtedy, gdy karnet jest udostępniony).

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

## 11. Wymagania niefunkcjonalne, bezpieczeństwo i testy

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

### Bezpieczeństwo (lista kontrolna według OWASP Top 10)

| Zagrożenie | Środek | Test |
| --- | --- | --- |
| Błędna kontrola dostępu (IDOR) | Polityki + sprawdzanie własności zasobu; `404` dla cudzych | Test: trener A prosi o zasób trenera B; klient o dane innego klienta |
| Niepoprawne uwierzytelnianie | Rotacja refresh tokenów, blokada konta, limit prób | Testy z rozdziału 7 (ponowne użycie, wygaśnięcie) |
| Wstrzykiwanie | Zapytania parametryzowane przez EF Core; brak sklejania SQL | Przegląd kodu, analiza statyczna |
| XSS | React escapuje treść; access token tylko w pamięci; nagłówek CSP | Test nagłówków odpowiedzi |
| CSRF | Ciasteczko `SameSite=Strict`, ograniczone do `/api/auth`; wymagany nagłówek niestandardowy | Test żądania z obcego źródła |
| Błędna konfiguracja | Nagłówki bezpieczeństwa (HSTS, `X-Content-Type-Options`), CORS tylko dla znanego źródła, brak szczegółów błędów w produkcji | Test integracyjny nagłówków |
| Podatne zależności | Dependabot, `dotnet list package --vulnerable`, `npm audit` w CI | Bramka w potoku |
| Ujawnianie danych w logach | Brak haseł, tokenów i pełnych e-maili w logach | Test sprawdzający logi z logowania |
| Nadużycia | Rate limiting na `login`, `refresh`, `register`, zaproszeniach | Test: seria żądań kończy się `429` |

### Dane osobowe

Aplikacja przechowuje dane osobowe (imię, e-mail, historia treningów, wpłaty). W projekcie szkoleniowym wystarczy potraktować to poważnie i zbudować podstawy: zgoda i polityka prywatności przy rejestracji, eksport własnych danych (JSON), usunięcie lub anonimizacja konta oraz wgląd admina zapisywany w logu audytu. To nie jest porada prawna; przy wdrożeniu produkcyjnym wymagane są konsultacje.

### Strategia testów

Piramida: najwięcej szybkich testów reguł domeny, mniej integracyjnych, najmniej end-to-end. Twoje doświadczenie z automatyzacją to tu największy atut, więc **testy powstają razem z kodem, a dla reguł z rozdziału 3 przed kodem (TDD)**.

| Poziom | Co sprawdza | Narzędzia | Przykłady |
| --- | --- | --- | --- |
| Jednostkowe (Domain) | Reguły zużywania sesji, daty ważności, statusy | xUnit `[Theory]`, Shouldly, `FakeTimeProvider` | Tabela przypadków z rozdziału 3 jako dane testowe |
| Jednostkowe (Application) | Przypadki użycia z atrapami repozytoriów | xUnit, NSubstitute | `CompleteSession` zmienia licznik i wywołuje powiadomienie |
| Integracyjne API | Endpoint → baza, autoryzacja, format błędów | `WebApplicationFactory`, Testcontainers, Respawn | Macierz RBAC jako test sterowany danymi: (endpoint, rola) → `200` / `403` / `404` |
| Kontrakt API | Brak niezamierzonych zmian łamiących | Migawka OpenAPI w repozytorium, różnica w CI | Zmiana pola w odpowiedzi psuje test, dopóki ktoś jej nie zatwierdzi |
| Zadania w tle | Przypomnienia, wygaszanie, idempotencja | Test integracyjny + `FakeTimeProvider` | Przesunięcie zegaru o 7 dni wysyła jedno przypomnienie, drugie uruchomienie nic nie dubluje |
| Komponenty React | Stany ładowania, pusty, błąd, formularze | Vitest, Testing Library, MSW | Pulpit klienta z 2 sesjami pokazuje ostrzeżenie `(!)` |
| End-to-end | Kluczowe ścieżki na uruchomionym stosie | Playwright (trzy przeglądarki, emulacja telefonu) | Lista poniżej |
| Niefunkcjonalne | Dostępność, wydajność | axe, k6 | Skan axe każdego ekranu; k6 na pulpicie trenera |

**Sześć ścieżek end-to-end (zestaw minimalny):**

1. Trener zaprasza klienta, klient rejestruje się z kodu i widzi pustą listę, a po utworzeniu pakietu licznik „10 z 10”.
2. Trener oznacza trzy sesje jako odbyte, a pulpit klienta pokazuje „zostało 7 z 10”.
3. Klient odwołuje sesję na 12 godzin przed startem przy progu 24 godzin: licznik spada i interfejs mówi dlaczego.
4. Access token wygasa w trakcie pracy: aplikacja odświeża sesję bez widocznej przerwy; po ponownym użyciu starego refresh tokenu użytkownik trafia na logowanie.
5. Klient próbuje otworzyć adres pakietu innej osoby i widzi „Nie znaleziono”.
6. Po przesunięciu zegaru do 7 dni przed końcem karnetu w Mailpit pojawia się dokładnie jedna wiadomość.

### Przykład kryterium akceptacji (Gherkin)

Każda funkcja z rozdziału 4 dostaje kryteria w takiej postaci; można je potem zamienić na testy.

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

## 12. Plan szkolenia: budowa krok po kroku

Specyfikacja jest za duża, by budować ją na raz, więc dzielimy ją na 10 etapów (0–9). Każdy etap kończy się działającym fragmentem aplikacji (pionowy plasterek: baza, API, ekran i testy), a nie samą warstwą. Czasy zakładają około 8 godzin tygodniowo i są tylko orientacyjne: ważniejsze od tempa jest spełnienie kryterium „Gotowe, gdy”.

Poniższa mapa grupuje etapy w cztery fazy z bramkami jakości. Bramka 3 oznacza działające MVP.

&#91;embedded content: mapa drogowa · 4 fazy, 4 bramki\]

Fazy nie mają stałych dat, tylko kryteria przejścia: do kolejnej fazy idziesz, gdy bramka jest spełniona, a nie gdy minie tydzień.

### Zasady pracy

1. **Plasterek pionowy.** Funkcję budujesz od bazy po ekran, a nie wszystkie tabele najpierw, a wszystkie ekrany potem.
2. **Test najpierw tam, gdzie jest reguła.** Reguły z rozdziału 3 piszesz jako czerwone testy, potem kod, potem porządki (red, green, refactor). Tu Twoja przewaga z automatyzacji procentuje najbardziej.
3. **Małe pull requesty.** Jeden PR to jedna funkcja z rozdziału 4 (np. `feat(TRN-05): przypisanie pakietu klientowi`), zwykle do 300 zmienionych linii.
4. **Najpierw zrozum, potem wklej.** Kod z dokumentacji, od kolegi czy z narzędzia AI wklejasz dopiero, gdy potrafisz powiedzieć, co robi każda linia. Dobrym sposobem jest prosić o przegląd własnego kodu zamiast o gotowe rozwiązanie.
5. **Debuguj, nie zgaduj.** Naucz się punktów przerwania w IDE, czytania stack trace i logów, zanim zaczniesz dodawać `Console.WriteLine`.
6. **Zamknięcie etapu.** Po każdym etapie krótka demonstracja (nagranie ekranu lub zrzuty), trzy zdania „czego się nauczyłem, co poszło źle, co zmienię” w `docs/dziennik.md` i ewentualny wpis ADR.

### Etap 0. Środowisko (około 1 tydzień)

- **Zbuduj:** repozytorium według rozdziału 9, rozwiązanie .NET z czterema projektami, szablon React (Vite + TypeScript), `docker-compose.yml` z PostgreSQL i Mailpit, endpoint `/health/live`, pierwszy potok CI (budowanie i pusty test).
- **Naucz się:** struktura solution, `dotnet` CLI, `npm`, Docker Compose, Git z pull requestami.
- **Gotowe, gdy:** `docker compose up`, uruchomione API i Vite pokazują stronę z odpowiedzią `/health/live`, a CI jest zielone.

### Etap 1. Model danych i reguły pakietu (około 2 tygodnie)

- **Zbuduj:** encje `Package`, `Session`, `PackageAdjustment`, `TrainerClient` w Domain z regułami z rozdziału 3; konfiguracje EF Core, pierwsza migracja, seeder danych demonstracyjnych.
- **Naucz się:** modelowanie domeny, EF Core i migracje, testy parametryzowane, `TimeProvider`.
- **Gotowe, gdy:** każda reguła zużywania sesji z rozdziału 3 ma test jednostkowy; liczenie `remaining`, `reserved`, `free` zgadza się z bazą w teście integracyjnym (Testcontainers).

### Etap 2. Rejestracja i logowanie (około 2 tygodnie)

- **Zbuduj:** AUTH-01 i AUTH-02 na ASP.NET Core Identity, jednolity format błędów (SYS-05), walidatory; w React strony `/login` i `/register` (S-01, S-02) z React Hook Form i Zod.
- **Naucz się:** potok żądania (middleware), wstrzykiwanie zależności, walidacja, formularze w React, hooki `useState` i `useEffect`.
- **Gotowe, gdy:** można założyć konto i zalogować się z przeglądarki; testy integracyjne obejmują błędne hasło, zajęty e-mail i słabe hasło; błędy walidacji widać przy polach.

### Etap 3. JWT i refresh token z rotacją (około 2 tygodnie)

- **Zbuduj:** wystawianie access tokenu, refresh token w ciasteczku `HttpOnly`, tabela `RefreshToken`, rotacja i wykrywanie ponownego użycia (AUTH-03, AUTH-04, AUTH-05); moduł `http` w React z jednym wspólnym odświeżaniem i `AuthProvider`.
- **Naucz się:** JWT od środka (nagłówek, claimy, podpis), kryptografia w praktyce (losowość, skróty), ciasteczka, CORS i proxy Vite, `Context` w React.
- **Gotowe, gdy:** testy dowodzą, że: stary token po rotacji unieważnia rodzinę, równoległe żądania wywołują jedno odświeżenie, a wygaszenie access tokenu nie przerywa pracy w przeglądarce (test Playwright).

### Etap 4. RBAC i wygenerowany klient API (około 1–2 tygodnie)

- **Zbuduj:** role i uprawnienia, polityki, handler własności zasobu (AUTH-06), dokumentacja OpenAPI i wygenerowany klient TypeScript (SYS-07), komponent `RequireRole`, ekran profilu z sesjami (S-03, część AUTH-10).
- **Naucz się:** autoryzacja oparta na politykach i zasobach, OpenAPI, generowanie kodu, chronione trasy w React Router.
- **Gotowe, gdy:** macierz z rozdziału 2 jest testem sterowanym danymi (endpoint × rola → `200` / `403` / `404`), a test IDOR potwierdza, że trener A nie widzi klienta trenera B.

### Etap 5. Klienci, oferta i pakiety (około 2–3 tygodnie)

- **Zbuduj:** zaproszenia (TRN-01, CLI-04), szablony pakietów (TRN-04), tworzenie pakietu (TRN-05), lista klientów z filtrem „do odnowienia” (TRN-02, TRN-03), pulpity (CLI-01–03), stronicowanie i sortowanie (SYS-06); ekrany K-01, K-02, T-01–T-04, T-06.
- **Naucz się:** zapytania EF Core (`Include`, rzutowanie na DTO, unikanie N+1), stronicowanie po stronie serwera, TanStack Query, formularze w oknach dialogowych.
- **Gotowe, gdy:** scenariusz E2E nr 1 przechodzi, a lista klientów z 1 000 rekordów odpowiada szybko (plan zapytania sprawdzony).

### Etap 6. Sesje, karnety i przypomnienia (około 3 tygodnie)

- **Zbuduj:** planowanie, oznaczanie i odwoływanie sesji z regułami 24 godzin (TRN-06–08, CLI-05), korekty pakietu, karnety klienta (CLI-08, CLI-09), zadania w tle z idempotencją i outbox (SYS-01–03), e-maile przez MailKit; ekrany T-05, K-03, K-04.
- **Naucz się:** transakcje i współbieżność (blokada wiersza), `BackgroundService`, wzorzec outbox, testy z `FakeTimeProvider`, wysyłka SMTP.
- **Gotowe, gdy:** testy współbieżności nie pozwalają zarezerwować ostatniej sesji dwukrotnie, a scenariusze E2E nr 2, 3 i 6 przechodzą. **To jest MVP.**

### Etap 7. Funkcje P1 i dopracowanie interfejsu (około 2–3 tygodnie)

- **Zbuduj:** prośba o termin i zakwestionowanie sesji (CLI-06, CLI-07, TRN-11), udostępnianie karnetu (CLI-10), płatności (TRN-09), kalendarz tygodnia (TRN-10), ustawienia przypomnień i trenera (CLI-11, TRN-12), powiadomienia w aplikacji (SYS-04), układ mobilny i tryb ciemny (SYS-12).
- **Naucz się:** złożone stany interfejsu, optymistyczne aktualizacje w TanStack Query, responsywność, dostępność (axe).
- **Gotowe, gdy:** wszystkie ekrany z rozdziału 5 mają stany ładowania, pusty i błędu, skan axe jest zielony, a testy na emulacji telefonu przechodzą.

### Etap 8. Utwardzanie i administracja (około 2 tygodnie)

- **Zbuduj:** blokada konta, potwierdzenie e-mail, reset hasła, lista urządzeń (AUTH-07–10), panel administratora i log audytu (ADM-01–04), rate limiting (SYS-09), logi strukturalne (SYS-08), nagłówki bezpieczeństwa, skanowanie zależności w CI.
- **Naucz się:** bezpieczeństwo aplikacji webowych (OWASP), logowanie i diagnostyka, ograniczanie żądań, audyt.
- **Gotowe, gdy:** lista kontrolna bezpieczeństwa z rozdziału 11 jest zielona, a logi z logowania nie zawierają haseł ani tokenów.

### Etap 9. Rozszerzenia i wdrożenie (około 2 tygodnie i więcej)

- **Zbuduj (do wyboru):** TOTP i logowanie przez Google (AUTH-11, AUTH-12), eksport ICS (CLI-13), raport miesięczny CSV (TRN-14), powtarzalne sesje (TRN-13), wersja EN (SYS-11), Hangfire zamiast własnych zadań, refaktoryzacja do Minimal API; wdrożenie kontenerów na wybranym hostingu z domeną i HTTPS.
- **Naucz się:** wdrożenia i konfiguracja produkcyjna, migracje na żywej bazie, monitoring, kopie zapasowe.
- **Gotowe, gdy:** aplikacja działa publicznie pod HTTPS, odtworzenie kopii bazy zostało przećwiczone, a potok CI wdraża zmiany po zielonym buildzie.

### Co dalej po etapie 9

To zestaw pomysłów, które nie wymagają zmian w modelu: pakiety grupowe i wielu trenerów na jednego klienta, bramka płatności online (np. webhook i zgodność z PSD2), integracja z kalendarzem Google, aplikacja mobilna (React Native lub PWA z powiadomieniami push), raporty dla właściciela siłowni.
