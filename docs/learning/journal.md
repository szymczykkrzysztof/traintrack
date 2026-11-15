# Dziennik nauki

Po każdym slice'ie Claude dopisuje szkic wpisu, użytkownik go poprawia własnymi słowami.

## Szablon wpisu

### Slice NN – nazwa (data)
**Trzy nowe pojęcia**
1. 
2. 
3. 

**Decyzja, której sam bym nie podjął – i dlaczego była dobra**


**Co nadal jest niejasne**


**Pytania kontrolne i moje odpowiedzi**
1. 
2. 
3. 

---

### Tydzień 1 – podział specyfikacji (2026-11-15)
**Trzy nowe pojęcia**
1. Identyfikowalność wymagań (traceability) – każdy plik wskazuje rozdział źródła.
2. Autoryzacja oparta na zasobie i zasada 404 zamiast 403 dla cudzych zasobów.
3. Wzorzec outbox i idempotencja przypomnień przez unikalny indeks w `ReminderLog`.

**Decyzja, której sam bym nie podjął – i dlaczego była dobra**
Nie scalać rozbieżnych fragmentów (strategia testów, rozdział 12) z istniejącymi dokumentami, tylko zgłosić je jako otwarte pytania – wybór wersji należy do PO.

**Co nadal jest niejasne**


**Pytania kontrolne i moje odpowiedzi**
1. Dlaczego zasada 404 zamiast 403 jest w security.md, a nie w roles-and-permissions.md?
2. Pozostałe i wolne sesje dla przykładu z K-02 z korektą +1 – czy reguła 1 i wzór z database.md dają ten sam wynik?
3. Dlaczego `ReminderLog`, `Notification` i `OutboxMessage` zapisują się w jednej transakcji?

---

### Tydzień 1 – sesja analityka (2026-11-15)
**Trzy nowe pojęcia**
1. Stan wyliczany – status pakietu i stan płatności wynikają z danych, nie są osobnym polem (BA-05, BA-06).
2. Kopia warunków zakupu (snapshot) – próg odwołania i sposób liczenia ważności nie zmieniają się wstecz (BA-12, BA-13).
3. Miękkie usunięcie a anonimizacja (BA-27, BA-33).

**Decyzja, której sam bym nie podjął – i dlaczego była dobra**
Sesje w sporze liczone jako zużyte przy sprawdzaniu korekty (BA-31) – jedna reguła usuwa konflikt między BA-02, BA-03 i BA-21.

**Co nadal jest niejasne**


**Pytania kontrolne i moje odpowiedzi**
1. Pakiet 10, zużyte 7, korekta +1, 1 zaplanowana, 1 w sporze, 1 prośba: pozostałe, zarezerwowane, wolne i największa korekta ujemna?
2. Pakiet „od pierwszej sesji”, 30 dni, zakup 01.10, pierwsze zużycie to późne odwołanie sesji z 05.10 – jaka `ExpiresOn` i które BA o tym decydują?
3. Dlaczego komunikat o nakładaniu się sesji (BA-28) nie może ujawnić trenera ani godziny? Którą regułę z CLAUDE.md by to łamało?

---

