# ADR-002: Modularny monolit i podział projektów backendu

- **Status:** Accepted
- **Data:** 2026-11-15
- **Decyduje:** użytkownik (Product Owner); przygotowuje: Claude

## Kontekst
- **Zespół i skala:** jedna osoba plus Claude, około 8–10 h tygodniowo. Wymagania wydajności to < 300 ms p95 przy 1 000 klientów na trenera ([overview.md](../overview.md)). Nie ma potrzeby niezależnego skalowania części systemu.
- **Obszary domeny:** konta, relacje trener–klient, pakiety i płatności, sesje, karnety, powiadomienia, administracja. Pakiet i sesja są silnie powiązane: licznik i rezerwacje w jednej transakcji (BA-01, BA-03, BA-22).
- **Sprzeczność w dokumentach o strukturze projektów, którą ten ADR rozstrzyga:**
  - CLAUDE.md: „modularny monolit”;
  - [overview.md](../overview.md) (spec, rozdział 9): cztery projekty warstwowe `Domain` / `Application` / `Infrastructure` / `Api`;
  - [00-walking-skeleton.md](../../../specs/foundation/00-walking-skeleton.md): `TrainTrack.Api` i `TrainTrack.Shared` oraz testy `UnitTests` / `IntegrationTests`;
  - testy w overview.md: `TrainTrack.Domain.Tests`, `TrainTrack.Application.Tests`, `TrainTrack.Api.IntegrationTests`.

## Rozważane opcje

| Opcja | Zalety | Wady |
| --- | --- | --- |
| A. Mikroserwisy (np. osobno konta, pakiety i sesje, powiadomienia) | Niezależne wdrażanie i skalowanie | Licznik i rezerwacja w jednej transakcji rozpadają się na transakcje rozproszone; wiele baz, kolejka, sieć – koszt wielokrotnie większy niż wartość przy tej skali |
| B. Monolit warstwowy: 4 projekty z overview.md, podział według warstw | Jasny kierunek zależności (Domain bez zależności); reguły testowalne bez bazy; zgodny ze specyfikacją | Kod jednej funkcji rozrzucony po 4 projektach; nic nie pilnuje granic między obszarami (np. karnety sięgające do encji pakietu) |
| C. Monolit warstwowy z modułami jako folderami: 4 projekty z B, w każdym foldery według obszarów (`Packages`, `Sessions`, `Memberships`, `Identity`, `Notifications`, `Admin`) i testy architektury pilnujące granic | Zalety B; pliki jednej funkcji leżą w tych samych folderach w każdej warstwie; granice modułów sprawdzane automatycznie w CI | Granice pilnowane testami, nie kompilatorem; trzeba ustalić, co moduł udostępnia innym |
| D. Modularny monolit „pełny”: osobny zestaw projektów (lub osobny `DbContext` / schemat) na każdy moduł | Najsilniejsze granice (kompilator, osobne schematy) | 15–20 projektów na start; pakiety i sesje i tak muszą dzielić transakcję, więc granica między nimi byłaby sztuczna; dużo ceremonii jak na naukę |

## Rekomendacja Claude
**Opcja C.** Daje to, co w modularnym monolicie najważniejsze: wyraźne obszary i pilnowane granice. Pakiety i sesje dalej mogą dzielić jedną transakcję i jeden `DbContext`. Zostaje też struktura ze specyfikacji, czyli `Domain` bez zależności i reguły testowane bez bazy. Opcja D ma sens, gdy moduły rozwijają różne zespoły, a tu tego nie ma.

Proponowana struktura (zastępuje różne wersje z dokumentów):

```text
src/backend/
├── TrainTrack.Domain/          Packages/, Sessions/, Memberships/, Identity/, ...
├── TrainTrack.Application/     te same foldery: handlery, DTO, walidatory, interfejsy
├── TrainTrack.Infrastructure/  EF Core (jeden AppDbContext), Identity, e-mail, zadania
└── TrainTrack.Api/             kontrolery, Program.cs, konfiguracja
tests/
├── TrainTrack.Domain.Tests/
├── TrainTrack.Application.Tests/
├── TrainTrack.Api.IntegrationTests/   WebApplicationFactory + Testcontainers
├── TrainTrack.Architecture.Tests/     granice warstw i modułów
└── e2e/                               Playwright
```

Zadania w tle działają na start w procesie `Api` jako `BackgroundService`. Osobny projekt `Worker` (wspomniany w overview.md) i wybór planisty zostawiam ADR-005.

## Decyzja
**Opcja C – monolit warstwowy (4 projekty) z modułami jako folderami i testami architektury**, ze strukturą katalogów z sekcji „Rekomendacja Claude”. Decyzja Product Ownera z 2026-11-15, zgodna z rekomendacją Claude.

## Konsekwencje
- **Zyskujemy:**
  - jedno wdrożenie i jedną bazę;
  - transakcje lokalne dla licznika;
  - czytelny podział według obszarów;
  - automatyczne sprawdzanie zależności (np. `Domain` nie odwołuje się do EF Core, moduł `Memberships` nie używa encji `Package`).
- **Tracimy:**
  - granice nie są wymuszane przez kompilator, tylko przez testy architektury.
- **Do zrobienia:**
  - poprawić „Zakres” w `specs/foundation/00-walking-skeleton.md` (projekty `Shared`, `UnitTests`, `IntegrationTests` zastąpić strukturą powyżej);
  - wybrać bibliotekę testów architektury (np. NetArchTest.eNhancedEdition albo ArchUnitNET);
  - ustalić zasadę komunikacji między modułami (np. przez interfejsy w `Application`, bez sięgania do cudzych encji).
