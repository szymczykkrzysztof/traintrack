# Reguły biznesowe

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 3.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.
> Uzupełniono o decyzje z sesji analityka (sekcja „Doprecyzowania”, BA-01…BA-33).

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
| `Active` | Są wolne sesje i pakiet jest w terminie | Tak |
| `Completed` | Zużyto wszystkie sesje | Nie |
| `Expired` | Minęła data ważności, zostały sesje | Nie (trener może przedłużyć) |
| `Cancelled` | Anulowany przez trenera | Nie |

Status wynika z licznika i daty ważności (BA-05). Płatność nie jest statusem pakietu, tylko osobnym stanem wyliczanym z wpłat (BA-06); `PendingPayment` usunięty.

### Statusy sesji

`Planned` → `Completed` | `CancelledByClient` | `CancelledByTrainer` | `NoShow`. Dodatkowo `Disputed`, gdy klient zakwestionował oznaczenie sesji jako odbytej.

Prośba klienta o termin: `Requested` → `Planned` (trener potwierdza) | `Rejected` (trener odrzuca) | `Withdrawn` (klient wycofuje) – BA-22, BA-30. Rozstrzygnięcie sporu: `Disputed` → `Completed` | `CancelledByTrainer` (BA-21).

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

1. Liczba pozostałych sesji = `TotalSessions` + suma korekt (`PackageAdjustment.Delta`) − liczba sesji ze statusem zużywającym; wolne do planowania = pozostałe − zaplanowane (patrz BA-01). **Nie przechowujemy** tej liczby w osobnej kolumnie, tylko liczymy ją (lub trzymamy jako kolumnę obliczaną), żeby nigdy nie rozjechała się z historią.
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
| Pakiet: zostały ≤ 2 sesje (pozostałe, nie wolne – BA-16) | 2 sesje | Klient i trener |
| Pakiet: zbliża się data ważności | 14 i 7 dni | Klient i trener |
| Pakiet: wygasł z niewykorzystanymi sesjami | w dniu wygaśnięcia | Klient i trener |
| Karnet: zbliża się koniec | 14, 7, 1 dzień | Klient (oraz trener, jeśli karnet udostępniony) |
| Zaplanowana sesja jutro | 24 h przed | Klient i trener |

Ta sama kombinacja (zdarzenie + próg + odbiorca) nigdy nie wysyła się dwa razy. To wymaga zapisu wysłanych przypomnień w bazie ([rozdział 10](../architecture/notifications.md)).

## Doprecyzowania z sesji analityka (2026-11-15)

Decyzje Product Ownera. Każda ma identyfikator `BA-NN`; przy sprzeczności z treścią wyżej rozstrzyga ta sekcja.

### BA-01. Wzór na pozostałe i wolne sesje

- **Pytanie:** reguła 1 pomijała korekty, a [database.md](../architecture/database.md) je uwzględniał.
- **Decyzja:** obowiązuje wzór z modelu danych:
  - Pozostało = `TotalSessions` + `SUM(PackageAdjustment.Delta)` − liczba sesji z `ConsumesSession = true`
  - Zarezerwowane = liczba sesji `Requested` (BA-22), `Planned` i `Disputed` (BA-20)
  - Wolne do planowania = Pozostało − Zarezerwowane
- `TotalSessions` się nie zmienia: to warunek z chwili zakupu. Korekta zawsze jest osobnym wpisem z powodem i trafia do audytu.
- **Przykład (test):** pakiet 10, zużyto 8, korekta +1, 1 zaplanowana → pozostało 3, zarezerwowane 1, wolne 2.


### BA-02. Korekta ujemna

- **Pytanie:** czy korekta może zejść poniżej liczby zaplanowanych sesji albo poniżej zera?
- **Decyzja:** korekta jest odrzucana (`409`) tylko wtedy, gdy Pozostało spadłoby poniżej 0 (sesje `Disputed` liczone jako zużyte – BA-31). Wolne do planowania mogą przez korektę stać się ujemne.
- **Skutek:** zaplanowane sesje zostają; przy wolnych ≤ 0 nie da się zaplanować nowej (reguła 2 pakietu).
- **Przykład (test):** pozostało 3, zaplanowane 2: korekta −2 przechodzi (pozostało 1, wolne −1); korekta −4 → `409`.

### BA-03. Rozliczenie sesji przy zerowym liczniku

- **Pytanie:** co, gdy po korekcie ujemnej (BA-02) zostaje więcej zaplanowanych sesji niż pozostałych, a trener rozlicza kolejną?
- **Decyzja:** oznaczenie sesji przez trenera jako odbytej albo nieobecności jest odrzucane (`409`), gdy pakiet ma 0 pozostałych sesji. Licznik pozostałych nigdy nie jest ujemny. Odwołanie przez klienta nie jest blokowane (BA-04).
- **Wyjście dla trenera:** przepięcie sesji do innego pakietu tej pary, zamiana na sesję jednorazową albo korekta +1.
- **Przykład (test):** pozostało 1, zaplanowane 2: pierwsza sesja oznaczona jako odbyta (pozostało 0), druga → `409`.
- **Do backlogu:** przepięcie sesji do innego pakietu / na jednorazową to nowa funkcja; dziś nie ma jej w [user-stories.md](user-stories.md) ani w [api.md](../architecture/api.md).

### BA-04. Późne odwołanie przy zerowym liczniku

- **Pytanie:** klient odwołuje sesję po terminie bezkosztowym, a pakiet ma 0 pozostałych sesji (efekt BA-02).
- **Decyzja:**
  - Odwołanie zawsze przechodzi. Sesja dostaje `CancelledByClient` z `ConsumesSession = false`.
  - Odpowiedź API i wpis w logu audytu odnotowują, że późne odwołanie nie zostało policzone.
  - Trener dostaje powiadomienie i sam decyduje o dalszym rozliczeniu.
- **Przykład (test):** pozostało 0, zaplanowana 1, próg 24 h, odwołanie 4 h przed startem → status `CancelledByClient`, `consumesSession: false`, pozostało 0, trener ma powiadomienie.
- **Do uzupełnienia:** nowy typ powiadomienia (np. `LateCancellationNotCounted`) w tabeli zdarzeń w [notifications.md](../architecture/notifications.md).


### BA-05. Powrót ze statusu Completed / Expired

- **Pytanie:** czy korekta +1 na pakiecie `Completed` albo przedłużenie pakietu `Expired` przywraca `Active`?
- **Decyzja:** status pakietu wynika z danych:

  | Warunek | Status |
  | --- | --- |
  | Pakiet anulowany | `Cancelled` (zawsze, nieodwracalnie) |
  | Pozostało = 0 | `Completed` |
  | Pozostało > 0 i data ważności minęła | `Expired` |
  | Pozostało > 0 i pakiet w terminie | `Active` |

  Korekta i przedłużenie, które zmieniają te warunki, zmieniają też status, również z powrotem na `Active`.
- **Przykłady (testy):**
  - `Completed` + korekta +1 → `Active`
  - `Expired` z 3 sesjami + przedłużenie → `Active`
  - `Cancelled` + korekta → `409`
- Status `PendingPayment` z tabeli „Statusy pakietu” zostaje usunięty (BA-06).

### BA-06. Stan płatności jako osobna cecha pakietu

- **Pytanie:** `PendingPayment` był statusem pakietu, a ekrany K-02 i T-04 pokazują płatność obok statusu.
- **Decyzja:** płatność nie jest częścią statusu pakietu (statusy: BA-05). Stan płatności jest wyliczany z sumy wpłat względem ceny pakietu:

  | Suma wpłat | Stan płatności |
  | --- | --- |
  | 0 | Nieopłacony |
  | > 0 i < cena | Częściowo opłacony |
  | ≥ cena | Opłacony |

- `RequirePaymentFirst` (ustawienie trenera) blokuje planowanie sesji w pakiecie, który nie jest opłacony. Nie zmienia statusu.
- **Przykłady (testy):**
  - cena 1200, wpłata 600 → Częściowo opłacony, status `Active`
  - przy `RequirePaymentFirst` planowanie → `409`
- Szczegóły: BA-07 (dialog T-04), BA-08 (wpłata częściowa), BA-09 (nadpłata).

### BA-07. Przełącznik płatności w dialogu „Nowy pakiet” (T-04)

- **Pytanie:** co robi wybór „opłacony” w T-04, skoro stan płatności wynika z wpłat (BA-06)?
- **Decyzja:**
  - „Opłacony” zapisuje razem z pakietem (w tej samej operacji) wpłatę na pełną cenę pakietu z dzisiejszą datą (w strefie trenera).
  - Dialog dostaje pole „Metoda” (np. gotówka, przelew).
  - „Oczekuje” nie tworzy wpłaty.
  - Jedynym źródłem stanu płatności są wpłaty.
- **Przykłady (testy):**
  - pakiet 1200 zł z „opłacony” → jedna wpłata 1200 zł, stan Opłacony
  - z „oczekuje” → brak wpłat, stan Nieopłacony
- **Do uzupełnienia:** pole „Metoda” na makiecie T-04 w [screens.md](screens.md); `POST /api/packages` przyjmuje opcjonalną wpłatę początkową ([api.md](../architecture/api.md)).

### BA-08. „Najpierw płatność” a wpłata częściowa

- **Pytanie:** czy przy `RequirePaymentFirst` wystarczy wpłata częściowa?
- **Decyzja:** nie. Planowanie sesji jest dozwolone dopiero w stanie Opłacony (suma wpłat ≥ cena). Trener, który przyjmuje zaliczki, wyłącza to ustawienie.
- **Przykłady (testy):** `RequirePaymentFirst` włączone:
  - wpłata 600 z 1200 → planowanie `409`
  - dopłata 600 → planowanie przechodzi

### BA-09. Nadpłata i korekta błędnej wpłaty

- **Pytanie:** co, gdy nowa wpłata podniosłaby sumę ponad cenę pakietu?
- **Decyzja:**
  - Suma aktywnych wpłat nie może przekroczyć ceny pakietu; taka wpłata → `409`.
  - Przedpłatę za kolejny pakiet zapisuje się na tamtym pakiecie.
  - Błędną wpłatę koryguje się zmianą jej statusu na Anulowana, nie usunięciem (reguła: dane rozliczeniowe nie są usuwane).
  - Anulowana wpłata nie wlicza się do sumy.
- **Przykłady (testy):**
  - cena 1200, wpłacono 1200, wpłata 200 → `409`
  - po anulowaniu wpłaty 1200 stan wraca do Nieopłacony
- **Do uzupełnienia:** encja `Payment` w [database.md](../architecture/database.md) nie ma pola statusu; potrzebny też endpoint anulowania wpłaty w [api.md](../architecture/api.md) (wpis do audytu).


### BA-10. Zaplanowane sesje a data ważności

- **Pytanie:** czy można przesunąć sesję poza datę ważności i co z nierozliczonymi sesjami po wygaśnięciu?
- **Decyzja:**
  - Przesunięcie sesji (`PATCH /api/sessions/{id}`) na termin po dacie ważności → `409`; najpierw trzeba przedłużyć pakiet. Ta sama zasada co przy planowaniu (reguła 3).
  - Data ważności jest włącznie: sesja w dniu `ExpiresOn` jest w terminie.
  - Sesję z terminem ≤ `ExpiresOn` można rozliczyć (odbyta, nieobecność) także po wygaśnięciu pakietu: liczy się termin sesji, nie dzień oznaczenia.
  - Zadanie wygaszające nie zmienia sesji `Planned`.
- **Przykłady (testy):**
  - ważny do 30.11: przesunięcie sesji z 28.11 na 02.12 → `409`
  - sesja 30.11 oznaczona jako odbyta 02.12 → przechodzi i zużywa sesję
- **Powiązane:** status po rozliczeniu wylicza się wg BA-05. „Dzień” liczony w strefie trenera (BA-19).

### BA-11. Skutki anulowania pakietu

- **Pytanie:** co dzieje się z zaplanowanymi sesjami i wpłatami, gdy trener anuluje pakiet?
- **Decyzja:**
  - Anulowanie wymaga powodu i trafia do logu audytu.
  - W tej samej operacji wszystkie sesje `Planned` pakietu przechodzą w `CancelledByTrainer` (bez zużycia), a klient dostaje powiadomienie.
  - Wpłaty zostają bez zmian jako historia; ewentualny zwrot pieniędzy odbywa się poza systemem.
  - Sesje już rozliczone się nie zmieniają.
- **Przykład (test):** pakiet z 2 sesjami `Planned` i wpłatą 1200 zł:
  - po anulowaniu: status `Cancelled`, obie sesje `CancelledByTrainer` z `ConsumesSession = false`, wpłata 1200 zł bez zmian, wpis w audycie z powodem
  - anulowanie bez powodu → `400`
- **Do uzupełnienia:** `POST /api/packages/{id}/cancel` przyjmuje `reason` ([api.md](../architecture/api.md)); nowy typ powiadomienia o anulowaniu pakietu w [notifications.md](../architecture/notifications.md).

### BA-12. Próg bezkosztowego odwołania jest warunkiem pakietu

- **Pytanie:** czy zmiana progu odwołania w ustawieniach trenera działa na sprzedane pakiety?
- **Decyzja:**
  - Pakiet przy utworzeniu kopiuje `CancellationWindowHours` z ustawień trenera, tak jak cenę i liczbę sesji (reguła 6).
  - Zmiana ustawienia dotyczy tylko nowych pakietów.
  - Granica jest włącznie: odwołanie dokładnie na próg przed startem nie zużywa sesji („najpóźniej 24 h przed startem”).
- **Przykłady (testy):** pakiet kupiony przy 24 h, trener zmienia próg na 48 h:
  - odwołanie 30 h przed → nie zużywa
  - odwołanie dokładnie 24 h przed → nie zużywa
  - 23 h 59 min przed → zużywa
- **Do uzupełnienia:** pole `CancellationWindowHours` w encji `Package` ([database.md](../architecture/database.md)).

### BA-13. Początek ważności pakietu

- **Pytanie:** reguła 3 mówi o ustawieniu szablonu, a model ma `ValidityStartsOn` w `TrainerSettings` i przełącznik w T-04. Nie wiadomo też, co znaczy „pierwsza sesja”.
- **Decyzja:**
  - Sposób liczenia ważności („od zakupu” / „od pierwszej sesji”) jest warunkiem pakietu.
  - Domyślna wartość pochodzi z `TrainerSettings.ValidityStartsOn`; trener może ją zmienić w T-04, a pakiet ją kopiuje (jak BA-12). Szablon tego pola nie ma.
  - „Od zakupu”: `ExpiresOn` = `PurchasedOn` + `ValidityDays`, ustalane przy utworzeniu.
  - „Od pierwszej sesji”: `ExpiresOn` = dzień pierwszej sesji zużywającej (BA-15) + `ValidityDays`, z limitem startu (BA-14). Do tego czasu `ExpiresOn` jest puste.
- **Przykłady (testy):** 90 dni „od pierwszej sesji”, zakup 02.09:
  - pierwsza sesja 05.09 oznaczona jako odbyta → `ExpiresOn` = 04.12
  - zaplanowana, ale nierozliczona sesja nie ustawia `ExpiresOn`
- **Powiązane:** limit startu – BA-14; które zdarzenie i który dzień – BA-15.

### BA-14. Limit startu ważności „od pierwszej sesji”

- **Pytanie:** pakiet „od pierwszej sesji” bez żadnej rozliczonej sesji nigdy by nie wygasł, nie dostawałby przypomnień i nie trafiałby do „do odnowienia”.
- **Decyzja:** ważność startuje od pierwszej rozliczonej sesji, ale najpóźniej w dniu `PurchasedOn + ValidityDays`:

  ```text
  Start     = MIN(data pierwszej rozliczonej sesji, PurchasedOn + ValidityDays)
  ExpiresOn = Start + ValidityDays
  ```

  Jeśli do dnia `PurchasedOn + ValidityDays` nie było rozliczonej sesji, `ExpiresOn` = `PurchasedOn + 2 × ValidityDays`.
- **Przykłady (testy):** 90 dni, zakup 02.09.2026:
  - brak sesji → `ExpiresOn` = 01.03.2027
  - pierwsza sesja 05.09 → `ExpiresOn` = 04.12.2026
  - pierwsza sesja 10.12, czyli po 01.12 → `ExpiresOn` = 01.03.2027
- **Skutek dla zadań w tle:** w dniu `PurchasedOn + ValidityDays` pakiet bez rozliczonej sesji dostaje `ExpiresOn`, więc obejmują go przypomnienia o ważności ([notifications.md](../architecture/notifications.md)).

### BA-15. Zdarzenie i dzień, które uruchamiają ważność

- **Pytanie:** czy późne odwołanie uruchamia ważność „od pierwszej sesji” i czy liczy się dzień terminu sesji, czy dzień oznaczenia?
- **Decyzja:**
  - Ważność uruchamia pierwsza sesja z `ConsumesSession = true`: odbyta, nieobecność albo późne odwołanie.
  - Liczy się dzień terminu sesji (`StartsAt` przeliczone na datę w strefie trenera – BA-19), nie dzień oznaczenia. Spójne z BA-10.
- **Przykłady (testy):** 90 dni:
  - sesja 05.09 oznaczona 07.09 → `ExpiresOn` = 04.12
  - pierwsza zużywająca to późne odwołanie sesji z 10.09 → `ExpiresOn` = 09.12

### BA-16. Próg „zostały ≤ 2 sesje” liczony z pozostałych

- **Pytanie:** reguły przypomnień i [notifications.md](../architecture/notifications.md) liczyły próg z wolnych, a makieta K-01 z pozostałych.
- **Decyzja:** ostrzeżenie `(!)` w interfejsie i przypomnienie „mało sesji” liczą się z pozostałych (Pozostało ≤ 2, wzór z BA-01), nie z wolnych. Sygnał oznacza, że pakiet się kończy i pora go odnowić. Makieta K-01 jest zgodna z tą decyzją.
- **Przykłady (testy):**
  - pozostało 4, zaplanowane 3, wolne 1 → brak ostrzeżenia
  - pozostało 2, zaplanowana 1 → ostrzeżenie i jedno przypomnienie
- **Do uzupełnienia:** warunek `free ≤ próg` w tabeli zdarzeń w [notifications.md](../architecture/notifications.md) zmienić na `remaining ≤ próg`.


### BA-17. Status klienta na liście klientów (T-02)

- **Pytanie:** kolumna „Status” w T-02 mieszała status pakietu ze stanem klienta, a przykład Ewy (0 z 10, „Wygasł”, bez daty) przeczył BA-05.
- **Decyzja:** kolumna pokazuje status klienta, wyliczany z jego pakietów u tego trenera według priorytetu z BA-29. „Najnowszy pakiet” w tabeli poniżej czytaj jako pakiet wybrany wg BA-29:

  | Status klienta | Warunek |
  | --- | --- |
  | Brak pakietu | Para trener–klient nie ma żadnego pakietu |
  | Do odnowienia | Najnowszy pakiet `Active` i spełnia kryteria „do odnowienia” (progi: BA-18) |
  | Aktywny | Najnowszy pakiet `Active`, kryteria niespełnione |
  | Zakończony | Najnowszy pakiet `Completed` |
  | Wygasł | Najnowszy pakiet `Expired` |

- **Przykład (test):** Ewa, najnowszy pakiet 0 z 10 → „Zakończony”, kolumna „Ważny do” pokazuje datę ważności tego pakietu.
- **Do uzupełnienia:**
  - poprawić wiersz Ewy na makiecie T-02 w [screens.md](screens.md).

### BA-18. Kryteria „do odnowienia”

- **Pytanie:** spec mówiła tylko „mało sesji lub bliska data ważności”, bez progów.
- **Decyzja:** pakiet jest „do odnowienia”, gdy ma status `Active` i spełnia jeden z warunków:
  - Pozostało ≤ 2 (jak BA-16), albo
  - do `ExpiresOn` zostało ≤ 14 dni.

  Progi są takie same jak dla ostrzeżenia `(!)` i przypomnień. Pakiet bez `ExpiresOn` (BA-13) spełnia tylko warunek sesji.
- **Przykłady (testy):**
  - Anna: 2 z 10, ważny jeszcze 40 dni → do odnowienia
  - Ola: 5 z 10, ważny jeszcze 12 dni → do odnowienia
  - 3 z 10, ważny jeszcze 15 dni → nie
- „Dziś” liczone w strefie trenera (BA-19).

### BA-19. Strefa czasowa dla dat pakietu i karnetu

- **Pytanie:** pakiet należy do pary trener–klient, a każde konto ma własne `TimeZoneId`. W czyjej strefie liczyć „dziś”, wygaśnięcie i dni do końca?
- **Decyzja:**

  | Co | Strefa | Dotyczy |
  | --- | --- | --- |
  | Daty pakietu i sesji | Strefa trenera | ważność, wygaszanie, „dni do końca”, dzień sesji (BA-10, BA-13–BA-15, BA-18); jedna data dla obu stron |
  | Daty karnetu | Strefa klienta | karnet należy do klienta |
  | Wyświetlanie godzin | Strefa oglądającego | wszystkie ekrany |

- **Przykład (test):** trener w Warszawie, klient w Londynie, pakiet ważny do 30.11:
  - o 00:30 1.12 czasu warszawskiego pakiet jest `Expired`, choć w Londynie jest jeszcze 30.11
- **Do uzupełnienia:** w [notifications.md](../architecture/notifications.md):
  - zadania wygaszania i przypomnień nie mogą liczyć „dziś” w jednej, stałej strefie `Europe/Warsaw`;
  - zasada „dziś w strefie użytkownika” dla przypomnień o pakietach zmienia się na strefę trenera.

### BA-20. Licznik w trakcie sporu i termin na zakwestionowanie

- **Pytanie:** czy sesja w sporze zwalnia miejsce do planowania i ile czasu klient ma na zakwestionowanie?
- **Decyzja:**
  - Sesja `Disputed` nie zużywa pakietu (`ConsumesSession = false`), ale rezerwuje miejsce jak sesja `Planned`.
  - Pozostałe rosną, a wolne do planowania się nie zmieniają.
  - Klient może zakwestionować tylko sesję `Completed`, najpóźniej 7 dni po jej terminie (`StartsAt` + 7 dni). Później przycisk znika, a API zwraca `409`.
- **Przykłady (testy):**
  - pozostało 1, wolne 1 → klient kwestionuje → pozostało 2, zarezerwowane +1, wolne 1
  - zakwestionowanie 8 dni po sesji → `409`
- Rozstrzygnięcie: BA-21.

### BA-21. Rozstrzygnięcie sporu

- **Pytanie:** co robi „cofnij” w `resolve-dispute` i co, gdy trener nie rozstrzyga?
- **Decyzja:**

  | Decyzja trenera | Nowy status sesji | Zużywa pakiet? |
  | --- | --- | --- |
  | Podtrzymaj | `Completed` | Tak |
  | Cofnij | `CancelledByTrainer` | Nie (reguła: odwołanie przez trenera nie zużywa) |

  - Obie decyzje wymagają komentarza, trafiają do logu audytu, a klient dostaje powiadomienie.
  - Nie ma automatycznego rozstrzygnięcia: dopóki trener nie zdecyduje, sesja zostaje `Disputed` i jest widoczna na pulpicie trenera.
- **Przykłady (testy):**
  - spór z BA-20 (pozostało 2): podtrzymaj → pozostało 1; cofnij → pozostało 2
  - decyzja bez komentarza → `400`
  - klient wywołuje `resolve-dispute` → `403`
- **Do uzupełnienia:**
  - sekcja „sesje w sporze” na makiecie T-01 w [screens.md](screens.md);
  - powiadomienie klienta o rozstrzygnięciu w [notifications.md](../architecture/notifications.md).
- Korekta ujemna w trakcie sporu: BA-31.

### BA-22. Prośba klienta o termin

- **Pytanie:** wśród statusów sesji nie było stanu „czeka na potwierdzenie” dla CLI-06; nie wiadomo też, czy prośba rezerwuje miejsce.
- **Decyzja:**
  - Nowy status sesji `Requested`:

    | Akcja | Wynik |
    | --- | --- |
    | Trener potwierdza | `Planned` |
    | Trener odrzuca (z powodem) | `Rejected` |
    | Klient wycofuje prośbę | `Withdrawn` (BA-30) |

  - Prośba rezerwuje miejsce jak sesja `Planned`, więc klient nie poprosi o więcej sesji, niż ma wolnych.
  - Przy prośbie obowiązują te same reguły co przy planowaniu: data ważności (reguła 3, BA-10), nakładanie się sesji, `RequirePaymentFirst` (BA-08).
- **Przykłady (testy):**
  - wolne 1 → prośba przechodzi (wolne 0) → druga prośba `409`
  - odrzucenie → wolne wraca do 1
  - prośba na termin po `ExpiresOn` → `409`
- **Do uzupełnienia:**
  - endpointy potwierdzenia i odrzucenia w [api.md](../architecture/api.md);
  - powiadomienie trenera o nowej prośbie.


### BA-23. Moment rozliczenia sesji

- **Pytanie:** czy sesję można oznaczyć przed jej startem i czy nierozliczone sesje zmieniają się same?
- **Decyzja:**
  - Oznaczenie jako odbytej albo nieobecności jest możliwe dopiero od `StartsAt`; wcześniej API zwraca `409`.
  - Nie ma automatycznej zmiany statusu: sesja `Planned` z terminem w przeszłości czeka na trenera.
  - Trener widzi takie sesje na pulpicie jako „do rozliczenia”.
- **Przykłady (testy):** sesja 22.10 18:00:
  - oznaczenie o 17:59 → `409`
  - o 18:00 → przechodzi
  - zadanie w tle nie zmienia sesji `Planned` z przeszłości (spójne z BA-10)
- **Do uzupełnienia:** sekcja „do rozliczenia” na makiecie T-01 w [screens.md](screens.md).

### BA-24. Udostępnianie karnetu wybranym trenerom

- **Pytanie:** model miał jedno pole `SharedWithTrainers` (tak/nie), a klient może mieć wielu trenerów.
- **Decyzja:**
  - Klient udostępnia każdy karnet wybranym trenerom spośród tych, z którymi ma aktywną relację.
  - Domyślnie karnet nie jest udostępniony nikomu.
  - Zakończenie relacji trener–klient automatycznie cofa udostępnienie temu trenerowi.
  - Trener widzi karnet tylko wtedy, gdy jest udostępniony właśnie jemu; w innym przypadku dostaje `404` (reguła 404 z [security.md](../architecture/security.md)).
- **Przykłady (testy):** Anna udostępnia karnet Markowi, nie Ewie:
  - Marek → `200`
  - Ewa → `404`
  - po zakończeniu relacji z Markiem → `404`
- **Do uzupełnienia:**
  - tabela udostępnień (np. `GymMembershipShare`: `MembershipId`, `TrainerId`) zamiast pola `SharedWithTrainers` w [database.md](../architecture/database.md);
  - `PUT /api/memberships/{id}/sharing` przyjmuje listę trenerów ([api.md](../architecture/api.md));
  - przełącznik w K-03 zamienić na wybór trenerów ([screens.md](screens.md)).

### BA-25. Archiwizacja klienta (koniec relacji trener–klient)

- **Pytanie:** co robi archiwizacja klienta (`PATCH /api/clients/{clientId}`) przy aktywnym pakiecie i kto może zakończyć relację?
- **Decyzja:**
  - Archiwizacja jest możliwa tylko wtedy, gdy para nie ma pakietu `Active`; w innym przypadku `409`. Trener najpierw anuluje pakiet (BA-11) albo czeka na jego koniec.
  - Archiwizacja kończy relację (`TrainerClient.Status = Archived`), cofa udostępnienia karnetów temu trenerowi (BA-24) i ukrywa klienta z domyślnej listy. Pakiety, sesje i wpłaty zostają bez zmian (dane rozliczeniowe nie są usuwane).
  - Ponowne zaproszenie i jego przyjęcie przywraca tę samą relację (`Active`).
  - W MVP relację kończy tylko trener; klient nie może sam „odłączyć” trenera.
- **Przykłady (testy):**
  - archiwizacja klienta z pakietem `Active` → `409`
  - z pakietem `Completed` → przechodzi; udostępniony karnet → `404` dla trenera
  - klient próbuje archiwizować → `403`
- Dostęp do historii po archiwizacji: BA-32.

### BA-26. Zaproszenie wiąże kod, nie adres e-mail

- **Pytanie:** czy zaproszenie wysłane na jeden adres może przyjąć konto z innym adresem?
- **Decyzja:**
  - Zaproszenie przyjmuje każdy zalogowany użytkownik, który zna ważny, jednorazowy kod. Adres e-mail służy tylko do wysłania kodu.
  - Trener widzi, kto przyjął zaproszenie (imię i e-mail konta); w razie pomyłki archiwizuje relację (BA-25).
  - Przyjęcie jest odrzucane (`409`), gdy kod przyjmuje sam zapraszający trener albo ktoś, kto ma już aktywną relację z tym trenerem.
  - Klient zarchiwizowany może przyjąć nowe zaproszenie, co przywraca relację (BA-25).
- **Przykłady (testy):**
  - zaproszenie na anna@example.com przyjęte przez anna.k@gmail.com → relacja powstaje, `Invitation.AcceptedByUserId` = to konto
  - drugie użycie kodu → `409`
  - trener przyjmuje własny kod → `409`

### BA-27. Usunięcie karnetu jest miękkie

- **Pytanie:** czy karnet siłowni podlega regule „dane rozliczeniowe nie są usuwane”, skoro CLI-08 przewiduje usunięcie?
- **Decyzja:** usunięcie karnetu zmienia jego status na `Deleted` zamiast kasować wiersz. Taki karnet:
  - znika ze wszystkich widoków i list API (dla klienta i trenerów; dla trenera `404`);
  - nie generuje przypomnień.
- **Przykłady (testy):** po `DELETE /api/memberships/{id}`:
  - wiersz istnieje ze statusem `Deleted`
  - `GET /api/memberships` go nie zwraca
  - przesunięcie zegara do 7 dni przed końcem → brak przypomnienia
- **Do uzupełnienia:** pole statusu w encji `GymMembership` ([database.md](../architecture/database.md)).
- Przywracanie i usunięcie konta: BA-33.

### BA-28. Sesje klienta też nie mogą się nakładać

- **Pytanie:** spec zakazywała nakładania się tylko sesji tego samego trenera; klient może mieć kilku trenerów.
- **Decyzja:**
  - Sesje jednego klienta w statusach `Requested` i `Planned` nie mogą nakładać się w czasie, także u różnych trenerów. W takim przypadku API zwraca `409`.
  - Komunikat nie ujawnia szczegółów cudzej sesji (trener, godzina, pakiet), tylko „klient ma w tym czasie inny trening”. Wynika to z reguły: trener widzi tylko swoje dane.
- **Przykłady (testy):** Marek ma z Anną sesję wt 18:30–19:30:
  - Ewa planuje Annie wt 18:00–19:00 → `409` z typem np. `/problems/client-session-overlap`, bez danych Marka
  - wt 19:30–20:30 → przechodzi (styk nie jest nakładaniem)
- **Do uzupełnienia:** ograniczenie w [database.md](../architecture/database.md) (punkt 5 „Ograniczenia i indeksy”) rozszerzyć o klienta.

### BA-29. Wybór pakietu dla statusu klienta

- **Pytanie:** który pakiet decyduje o statusie klienta (BA-17), gdy klient ma kilka pakietów u tego samego trenera, np. kończący się i kupiony na zapas?
- **Decyzja:**
  - Status klienta to najlepszy status spośród jego pakietów u tego trenera, w kolejności: Aktywny > Do odnowienia > Wygasł > Zakończony > Brak pakietu. Pakiety `Cancelled` się pomija.
  - Kolumny „Pakiet”, „Zostało” i „Ważny do” pokazują pakiet, z którego planuje się sesje: najstarszy aktywny (reguła 7).
- **Przykłady (testy):**
  - A (Active, 2 z 10) + B (Active, 10 z 10) → status „Aktywny”, kolumny pokazują A
  - tylko A (2 z 10) → „Do odnowienia”
  - tylko pakiet `Cancelled` → „Brak pakietu”

### BA-30. Wycofanie prośby przez klienta

- **Pytanie:** jaki status dostaje prośba `Requested` wycofana przez klienta przed decyzją trenera?
- **Decyzja:** nowy status `Withdrawn`. Nie zużywa i nie rezerwuje miejsca. W historii różni się od odrzucenia przez trenera (`Rejected`) i od odwołania sesji (`CancelledByClient`). Wycofać można tylko prośbę `Requested`.
- **Przykłady (testy):**
  - wolne 0 po prośbie → wycofanie → status `Withdrawn`, wolne 1
  - wycofanie sesji `Planned` → `409` (to jest odwołanie, nie wycofanie)

### BA-31. Korekta ujemna w trakcie sporu

- **Pytanie:** korekta ujemna w trakcie sporu mogła sprowadzić pozostałe do 0, a późniejsze podtrzymanie sporu (BA-21) zeszłoby do −1.
- **Decyzja:** przy sprawdzaniu korekty (BA-02) sesje `Disputed` liczy się tak, jakby były zużyte. Dzięki temu podtrzymanie sporu nigdy nie sprowadzi licznika poniżej zera i nie koliduje z BA-03.
- **Przykład (test):** pozostało 2, w tym 1 sesja w sporze:
  - korekta −2 → `409`
  - korekta −1 → przechodzi
  - potem podtrzymanie → pozostało 0

### BA-32. Historia zarchiwizowanego klienta

- **Pytanie:** czy trener widzi pakiety, sesje i wpłaty klienta po archiwizacji relacji (BA-25)?
- **Decyzja:**
  - Tak, tylko do odczytu. Lista klientów (T-02) ma filtr „Zarchiwizowani”.
  - Trener widzi historię pakietów, sesji i wpłat zarchiwizowanego klienta, ale nie widzi jego karnetów (BA-24).
  - Każda zmiana (nowy pakiet, sesja, wpłata, korekta) jest odrzucana, dopóki relacja nie zostanie przywrócona (BA-26).
- **Przykłady (testy):** po archiwizacji Anny:
  - `GET /api/clients/{id}` i `GET /api/packages?clientId=` → `200`
  - `POST /api/packages` dla Anny → `409`
  - karnet Anny → `404`
  - inny trener → `404` (bez zmian)
- **Do uzupełnienia:** filtr „Zarchiwizowani” na makiecie T-02 w [screens.md](screens.md).

### BA-33. Brak przywracania karnetu; usunięcie konta

- **Pytanie:** czy usunięty karnet (BA-27) można przywrócić i co dzieje się z karnetami przy usunięciu konta?
- **Decyzja:**
  - W MVP nie ma przywracania karnetu `Deleted`; klient dodaje karnet od nowa.
  - Usunięcie konta klienta ([security.md](../architecture/security.md), „Dane osobowe”):
    - fizycznie kasuje wszystkie jego karnety (również `Deleted`) i ich udostępnienia, bo to dane prywatne, a nie rozliczenie w TrainTrack;
    - pakiety, sesje i wpłaty są anonimizowane, a nie usuwane (dane rozliczeniowe nie są usuwane).
- **Przykłady (testy):**
  - po usunięciu konta klienta w bazie nie ma jego wierszy `GymMembership`
  - jego pakiety i wpłaty istnieją, ale bez danych osobowych
  - trener widzi je jako klienta zanonimizowanego

