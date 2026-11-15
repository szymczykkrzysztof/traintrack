# ADR-001: PostgreSQL jako baza danych

- **Status:** Accepted
- **Data:** 2026-11-15
- **Decyduje:** użytkownik (Product Owner); przygotowuje: Claude

## Kontekst
TrainTrack potrzebuje relacyjnej bazy z transakcjami, bo licznik sesji, rezerwacje i wpłaty muszą być spójne (BA-01, BA-02, BA-09). Dostęp przez EF Core code-first z migracjami. Ograniczenia:

- **Koszt:** projekt szkoleniowy, docelowo tani hosting demo (etap 9).
- **Środowisko:** rozwój na Macu z procesorem ARM (arm64), Docker Compose lokalnie, Testcontainers w testach integracyjnych i w GitHub Actions.
- **Wymagania z modelu danych** ([database.md](../database.md)):
  - token współbieżności (`xmin`);
  - zakaz nakładania się sesji trenera i klienta (BA-28), najprościej jako ograniczenie bazy;
  - indeksy częściowe (karnety `Active`);
  - kolumny JSON (`DataJson`, `ThresholdsJson`);
  - `timestamptz` i `DateOnly`.
- **Cel szkoleniowy:** umiejętności przydatne na rynku pracy .NET.

## Rozważane opcje

| Opcja | Zalety | Wady |
| --- | --- | --- |
| A. PostgreSQL + Npgsql | Darmowy, także w wersjach zarządzanych u większości dostawców; natywny obraz arm64, więc szybki start w Testcontainers na Macu; `EXCLUDE USING gist` wyraża BA-28 jednym ograniczeniem; `xmin` daje token współbieżności bez dodatkowej kolumny; `jsonb`, indeksy częściowe, `timestamptz` | Mniej popularny niż SQL Server w firmowych projektach .NET; część zachowań Npgsql różni się od dokumentacji EF Core pisanej pod SQL Server (np. mapowanie `xmin`, strefy w `timestamptz`) |
| B. SQL Server (Express / Developer) + EF Core | Najczęstszy wybór w firmach .NET, więc przydatny na rynku pracy; najlepiej opisany w dokumentacji EF Core; `rowversion` jako token współbieżności | Obrazy kontenera są tylko amd64, więc na Macu ARM działają przez emulację (wolniejszy start testów); Express ma limit rozmiaru bazy, a hosting zarządzany jest zwykle droższy; brak odpowiednika `EXCLUDE`, więc BA-28 trzeba robić w serwisie z blokadą albo triggerem |
| C. SQLite | Zero infrastruktury, plik na dysku | Słaba współbieżność zapisu i brak `SELECT … FOR UPDATE`, więc nie sprawdzi BA-02 i BA-09 pod obciążeniem; testy nie odzwierciedlałyby produkcji |

## Rekomendacja Claude
**Opcja A, PostgreSQL.** Przesądzają trzy rzeczy:
- Przy pracy na Macu ARM testy integracyjne startują natywnie, bez emulacji, a to te testy uruchamiasz najczęściej.
- Reguła BA-28 (sesje nie mogą się nakładać) staje się ograniczeniem bazy, a nie tylko kodem w serwisie, więc chroni dane także przy wyścigu dwóch żądań.
- Koszt hostingu demo jest najniższy.

Minus rynkowy (SQL Server w firmach) jest mniejszy, niż się wydaje. EF Core ukrywa większość różnic, a umiejętności transakcji, blokad i indeksów przenoszą się między bazami.

## Decyzja
**Opcja A – PostgreSQL + Npgsql.** Decyzja Product Ownera z 2026-11-15, zgodna z rekomendacją Claude.

## Konsekwencje
- **Zyskujemy:**
  - jedną technologię lokalnie, w CI i w produkcji;
  - ograniczenia integralności w bazie (`EXCLUDE`, `CHECK`, indeksy częściowe).
- **Tracimy:**
  - część dokumentacji i przykładów EF Core pisanych pod SQL Server; trzeba sprawdzać dokumentację Npgsql.
- **Do zrobienia:**
  - pakiet `Npgsql.EntityFrameworkCore.PostgreSQL`;
  - rozszerzenie `btree_gist` w pierwszej migracji, która doda ograniczenie nakładania;
  - konwencja nazw kolumn i tabel (np. `snake_case`) do ustalenia w slice'ie 0;
  - obraz `postgres` z przypiętą wersją w `docker-compose.yml` i w Testcontainers.
