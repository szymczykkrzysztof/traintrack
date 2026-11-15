# Ekrany

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 5.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

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

Lista urządzeń to widok tabeli refresh tokenów ([rozdział 7](../architecture/security.md)): każdy wiersz to jedna rodzina tokenów.

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

**K-02 Szczegóły pakietu** (kolumna „Zużywa” wynika z reguł z [rozdziału 3](business-rules.md), klient widzi, dlaczego licznik się zmienił)

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
- **Brak uprawnień:** ekran `403`, a dla cudzego zasobu ekran „Nie znaleziono” (zgodnie z regułą 404 z [rozdziału 2](../architecture/security.md)).
- **Kolory nie niosą znaczenia samodzielnie:** zawsze towarzyszy im tekst lub ikona (dostępność).
- **Konflikt:** gdy ktoś zmienił dane w międzyczasie, interfejs pokazuje komunikat o konflikcie i odświeża widok (patrz kontrola współbieżności, [rozdział 6](../architecture/database.md)).
