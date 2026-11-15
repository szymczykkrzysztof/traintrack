# Reguły biznesowe

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 3.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

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

Ta sama kombinacja (zdarzenie + próg + odbiorca) nigdy nie wysyła się dwa razy. To wymaga zapisu wysłanych przypomnień w bazie ([rozdział 10](../architecture/notifications.md)).
