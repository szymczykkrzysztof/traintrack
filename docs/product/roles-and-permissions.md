# Role i uprawnienia

> Źródło: [spec-traintrack.md](../product/spec-traintrack.md) (eksport z 21.10.2026), rozdział 2 (bez „Jak to zapisać w kodzie” – patrz [security.md](../architecture/security.md)).
> Treść przeniesiona bez zmian merytorycznych; odwołania do rozdziałów zamienione na linki.

## 2. Role, uprawnienia i macierz RBAC

Dostęp opiera się na trzech rolach, a każda rola to zestaw nazwanych uprawnień (permissions). Sama rola nie wystarcza: trener widzi dane **tylko swoich** klientów, a klient **tylko swoje**. Dlatego każdy endpoint sprawdza dwie rzeczy: czy rola ma uprawnienie oraz czy użytkownik jest właścicielem zasobu (autoryzacja oparta na zasobie).

### Role

| Rola | Kim jest | Zasada ogólna |
| --- | --- | --- |
| Client | Osoba trenująca | Widzi i zmienia wyłącznie własne dane, pakiety i karnety |
| Trainer | Trener personalny | Zarządza swoją ofertą, klientami, pakietami i sesjami; nie widzi karnetów klienta bez jego zgody |
| Admin | Właściciel systemu | Zarządza kontami i rolami, nie wchodzi w treningi (wgląd tylko do odczytu, każdy wgląd trafia do logu audytu) |

Jeden użytkownik może mieć kilka ról (trener, który sam też trenuje u innego trenera, ma Trainer + Client). Token zawiera wszystkie jego role, a interfejs pozwala przełączać widok.

### Macierz uprawnień

Legenda: **W** = własne zasoby, **K** = zasoby swoich klientów, **T** = wszystkie, **–** = brak dostępu.

| Zasób / akcja | Client | Trainer | Admin |
| --- | --- | --- | --- |
| Własny profil: odczyt i edycja | W | W | W |
| Zaproszenie klienta (e-mail + kod) | – | W | – |
| Przyjęcie zaproszenia | W | – | – |
| Lista klientów trenera | – | K | T (odczyt) |
| Szablon pakietu (oferta): CRUD | – | W | – |
| Pakiet: podgląd | W | K | T (odczyt) |
| Pakiet: utworzenie dla klienta | – | K | – |
| Pakiet: korekta ręczna liczby sesji (z powodem) | – | K | – |
| Pakiet: anulowanie | – | K | – |
| Sesja: podgląd | W | K | T (odczyt) |
| Sesja: zaplanowanie | prośba (W) | K | – |
| Sesja: oznaczenie odbyta / nieobecność | – | K | – |
| Sesja: odwołanie | W | K | – |
| Sesja: zakwestionowanie odbycia | W | – | – |
| Płatność za pakiet: zapis | – | K | – |
| Karnet na siłownię: CRUD | W | – | – |
| Karnet: podgląd przez trenera (po zgodzie klienta) | – | K, tylko jeśli udostępniony | – |
| Ustawienia przypomnień | W | W | – |
| Sesje logowania (urządzenia): podgląd i wylogowanie | W | W | T (wymuszone wylogowanie) |
| Użytkownicy: lista, blokada, zmiana ról | – | – | T |
| Log audytu | – | – | T |
