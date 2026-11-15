# Cykl pracy nad jednym slice'em

Każdy slice = jedna gałąź `feature/NN-nazwa`, jeden PR, działająca funkcja od UI do bazy.
Po każdym kroku jest bramka akceptacji użytkownika (docs/development/review-protocol.md).

| Krok | Claude | Użytkownik | Bramka |
| --- | --- | --- | --- |
| 1. Spec | Pisze `specs/<obszar>/NN-nazwa.md` wg `specs/_template.md` | Uzupełnia reguły biznesowe | Akceptacja specyfikacji |
| 2. Plan | Plan Mode: pliki, kroki, zależności, ryzyka | Ocenia kolejność i zakres | Akceptacja planu |
| 3. Przypadki testowe | Tabela: pozytywne, negatywne, brzegowe, autoryzacja | Dopisuje brakujące (doświadczenie QA) | Akceptacja listy |
| 4. Testy | Pisze testy, uruchamia, pokazuje czerwone wyniki, objaśnia | Czyta testy i asercje | Akceptacja testów |
| 5. Implementacja | Jeden krok planu, zielone testy, objaśnienie | Czyta diff i objaśnienie | Akceptacja kroku |
| 6. Weryfikacja | Build, wszystkie testy, uruchomienie aplikacji | Sprawdza działanie w UI | Potwierdzenie |
| 7. Review niezależny | Subagenci recenzują diff, Claude priorytetyzuje | Decyduje: teraz czy backlog | Decyzja |
| 8. Commit + PR | Commit, opis PR, status.md, dziennik | Merge do `develop` | Merge |

## Prompty

### 1. Plan
```
Read CLAUDE.md, docs/development/status.md and specs/<obszar>/<NN-nazwa>.md.
Explore the codebase in Plan Mode. Do not modify any files.
Prepare an implementation plan split into small steps: affected modules,
entities, endpoints, components, dependencies and risks.
Explain the plan so I can accept or change it.
```

### 2. Przypadki testowe
```
Based on the acceptance criteria, list test cases in a table:
level (unit / integration / E2E), scenario, input, expected result.
Include negative, boundary and authorization cases.
Do not write test code yet. Wait for my acceptance.
```

### 3. Testy
```
Write the accepted test cases. Do not implement production code.
Run them and show they fail for the right reason.
Explain each test: what it protects, why this assertion,
how test data is isolated. Wait for my acceptance.
```

### 4. Implementacja krok po kroku
```
Implement step 1 of the accepted plan only. Follow existing conventions,
no unrelated refactoring. Run the relevant tests.
Then explain the change using docs/development/review-protocol.md
and stop. Do not commit until I write: akceptuję.
```

### 5. Niezależny review
```
Ask the backend-reviewer and qa-engineer agents to independently review
the current git diff against the spec. No agent may modify files.
Report actual defects, grouped by severity, each with a failure scenario.
Explain each finding in plain terms and recommend: fix now or backlog.
```

### 6. Pogłębienie
```
Explain [plik / metoda / linia] deeper, as if mentoring a junior .NET developer.
Show what happens step by step at runtime, what would break if we removed it,
and link the relevant Microsoft docs. Do not change any code.
```

## Kiedy Plan Mode jest obowiązkowy

| Zadanie | Sposób pracy |
| --- | --- |
| Nowy slice | Plan → przypadki testowe → testy → implementacja |
| Zmiana architektury, nowy moduł | Plan Mode + ADR |
| Zmiana schematu DB | Plan → migracja → objaśnienie SQL → testy |
| Bug | Test odtwarzający błąd → poprawka → objaśnienie przyczyny |
| Drobna zmiana UI | Krótkie objaśnienie, bez rozbudowanego planu |

## Zasady kontroli

1. Małe kroki – jedno polecenie = jeden krok planu.
2. Akceptacja = zrozumienie.
3. Commit po każdym zaakceptowanym kroku.
4. Migracje, infrastruktura, CI i operacje destrukcyjne zawsze za zgodą.
5. Nowy slice = nowa sesja; źródłem prawdy są docs/, specs/ i status.md.
