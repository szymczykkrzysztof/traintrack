# Product brief

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 1 oraz „Co dalej po etapie 9” z rozdziału 12.
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 1. Cel, kontekst i zakres

**TrainTrack** to aplikacja internetowa, która pilnuje dwóch rzeczy: ile treningów zostało w pakiecie u trenera personalnego oraz kiedy kończy się karnet na siłownię. Klient i trener widzą ten sam, jeden stan pakietu, a system przypomina o odnowieniu, zanim pakiet lub karnet wygaśnie.

Dokument ma dwa zadania: jest specyfikacją produktu oraz punktem wyjścia do praktycznego szkolenia z budowy aplikacji fullstack (.NET + React). Dlatego każdy rozdział kończy się czymś, co można zbudować i przetestować, a [rozdział 12](spec-traintrack.md#12-plan-szkolenia-budowa-krok-po-kroku) układa to w kolejne etapy.

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

## 12. Kierunki rozwoju (fragment rozdziału 12)

### Co dalej po etapie 9

To zestaw pomysłów, które nie wymagają zmian w modelu: pakiety grupowe i wielu trenerów na jednego klienta, bramka płatności online (np. webhook i zgodność z PSD2), integracja z kalendarzem Google, aplikacja mobilna (React Native lub PWA z powiadomieniami push), raporty dla właściciela siłowni.
