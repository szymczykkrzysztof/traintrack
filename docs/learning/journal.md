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
