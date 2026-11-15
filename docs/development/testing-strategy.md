# Strategia testów

Testy pisze Claude. Użytkownik (doświadczenie QA) akceptuje listę przypadków przed kodem testów
i sprawdza, czy każdy test naprawdę może upaść.

## Piramida

| Poziom | Narzędzia | Co testujemy | Ile |
| --- | --- | --- | --- |
| Unit (backend) | xUnit | Licznik pakietu, okno odwołania, wyliczanie `ExpiresOn`, progi przypomnień | Najwięcej |
| Unit (frontend) | Vitest | Formatowanie dat w strefie, mapowanie statusów | Średnio |
| Komponenty | React Testing Library | Pulpit klienta: ładowanie, błąd, pusty stan, dane | Średnio |
| Integracja | xUnit + WebApplicationFactory + Testcontainers (PostgreSQL) | Endpointy, migracje, autoryzacja zasobowa, współbieżność | Dużo |
| Kontrakt | OpenAPI + generowane typy TS | Zgodność DTO backend–frontend | Automatycznie |
| E2E | Playwright + TypeScript | 5 krytycznych ścieżek | Najmniej |

## Testy obowiązkowe

- **Izolacja danych:** dla każdego endpointu z zasobem – trener B nie odczyta ani nie zmieni
  pakietu klienta trenera A; klient nie zobaczy cudzego karnetu.
- **Spójność licznika:** pozostałe treningi zawsze równe wyliczeniu z historii, także po korektach i odwołaniach.
- **Współbieżność:** dwie równoczesne próby oznaczenia ostatniej sesji jako odbytej – jedna przechodzi, druga dostaje 409.
- **Czas:** granica dnia (23:30 w Warszawie vs UTC) i zmiana czasu letniego – przez wstrzykiwany `TimeProvider`.
- **Tokeny:** ponowne użycie zrotowanego refresh tokenu unieważnia całą rodzinę.

## Krytyczne ścieżki E2E

1. Trener zaprasza klienta → klient rejestruje się z kodem → obaj widzą relację.
2. Trener przypisuje pakiet → klient widzi licznik i ważność.
3. Trener planuje i oznacza sesję → licznik klienta maleje.
4. Klient odwołuje sesję poza oknem → licznik bez zmian.
5. Klient dodaje karnet i udostępnia go → trener go widzi.

## Zasady

- Dane testowe zakładane przez API, nie przez UI.
- Page Object Model w Playwright; zero `waitForTimeout`.
- Prawdziwy PostgreSQL (Testcontainers) zamiast mocków bazy.
- Nazwy testów opisują zachowanie: `Client_cannot_see_other_clients_membership`.
- Test przed implementacją; czerwony z właściwego powodu.
