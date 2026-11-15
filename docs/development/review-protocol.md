# Protokół objaśniania i akceptacji

Claude pisze kod i testy. Użytkownik akceptuje każdy krok. Akceptacja oznacza zrozumienie.

## Po każdym kroku Claude podaje

1. **Co zmieniłem** – lista plików, po jednym zdaniu na plik.
2. **Dlaczego tak** – wybrane podejście i rozważane alternatywy.
3. **Przepływ żądania** – React → API → serwis/handler → EF Core → PostgreSQL i z powrotem.
4. **Nowe pojęcia** – elementy .NET / React / testowe użyte w projekcie po raz pierwszy,
   z linkiem do dokumentacji (Microsoft Learn, react.dev, playwright.dev).
5. **Ryzyka** – na co patrzeć w review, co może się zepsuć w produkcji.
6. **Jak sprawdzić samemu** – komenda do uruchomienia lub kliknięcie w UI.
7. **Trzy pytania kontrolne** – sprawdzają zrozumienie, odpowiedzi nie są podane od razu.

## Odpowiedzi użytkownika

| Odpowiedź | Co robi Claude |
| --- | --- |
| `akceptuję` | Commit (Conventional Commits) i przejście do następnego kroku |
| `wyjaśnij: <co>` | Głębsze objaśnienie, bez zmian w kodzie |
| `zmień: <co>` | Poprawka, potem nowe objaśnienie |
| `odrzucam: <dlaczego>` | Wycofanie kroku i nowa propozycja |

## Zasady

- Jeden krok planu na raz, potem stop.
- Nic nie trafia do commita bez „akceptuję”.
- Testy przed implementacją; pokaż, że failują z właściwego powodu.
- Na żądanie test mutacyjny: celowo zepsuj regułę, pokaż który test upada, wycofaj zmianę.
- Odpowiedzi na pytania kontrolne oceniaj szczerze; jeśli są błędne – wyjaśnij jeszcze raz.

## Checklista akceptacji kodu (dla użytkownika)

1. Czy umiem jednym zdaniem powiedzieć, co robi ta zmiana?
2. Czy wiem, skąd przychodzą dane i dokąd trafiają?
3. Czy wiem, co się stanie przy błędzie, braku danych i braku uprawnień?
4. Czy zmiana trzyma się planu i nie dotyka niezwiązanych plików?
5. Czy istnieje test, który upadłby, gdyby ten kod był błędny?

## Checklista akceptacji testów (dla użytkownika)

- Nazwa testu opisuje zachowanie biznesowe, nie metodę.
- Czytelne Arrange / Act / Assert; asercja na wynik, nie na szczegóły implementacji.
- Test był czerwony przed implementacją.
- Dane izolowane, brak `sleep` i zależności od kolejności.
- Są scenariusze negatywne, brzegowe i autoryzacji.
