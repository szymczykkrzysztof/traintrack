# TrainTrack

Aplikacja dla trenera personalnego i osoby trenującej: wspólny licznik treningów w pakiecie,
historia sesji, karnety na siłownię i przypomnienia przed wygaśnięciem.

Stos: ASP.NET Core + PostgreSQL + EF Core, React + TypeScript + Vite, xUnit, Testcontainers, Playwright.

Projekt powstaje z Claude Code: Claude pisze kod i testy, a ja akceptuję każdy krok po objaśnieniu
(docs/development/review-protocol.md).

## Zawartość startowa

| Ścieżka | Po co |
| --- | --- |
| `CLAUDE.md` | Zasady projektu – Claude Code wczytuje je w każdej sesji |
| `docs/development/review-protocol.md` | Jak Claude objaśnia zmiany i jak je akceptuję |
| `docs/development/workflow.md` | Cykl pracy nad slice'em + gotowe prompty |
| `docs/development/backlog.md` | Slice'y, harmonogram, ADR-y |
| `docs/development/testing-strategy.md` | Piramida testów i testy obowiązkowe |
| `docs/development/status.md` | Pamięć między sesjami |
| `docs/learning/journal.md` | Dziennik nauki |
| `docs/product/` | Tu trafia eksport specyfikacji (`spec-traintrack.md`) |
| `docs/architecture/decisions/` | ADR-y (szablon w ADR-000) |
| `specs/` | Specyfikacje slice'ów (szablon + slice 0) |
| `.claude/agents/` | 4 recenzentów tylko do odczytu |
| `.claude/skills/` | Procedury: write-spec, test-cases, vertical-slice, explain-change, close-session |
| `.claude/settings.json` | Uprawnienia: co wolno, o co pytać, czego nigdy |

## Start

1. Sprawdź narzędzia:
   ```
   dotnet --version && node -v && docker compose version && git --version && claude --version
   ```
2. Rozpakuj ten folder, np. do `~/Projects/traintrack`, i zainicjuj repo:
   ```
   cd ~/Projects/traintrack
   git init -b main && git add . && git commit -m "chore: project starter"
   git checkout -b develop
   ```
3. Wyeksportuj specyfikację z Claude (Export → Markdown) i zapisz jako
   `docs/product/spec-traintrack.md`.
4. Uruchom `claude` w katalogu projektu.

## Uruchomienie lokalne (po slice'ie 0)

Wymagania: .NET SDK z `global.json`, Node 24 (`.nvmrc`), Docker.

```
# baza (dostępna tylko z localhost)
docker compose up -d --wait db

# connection string poza repozytorium (hasło deweloperskie z docker-compose.yml)
dotnet user-secrets set "ConnectionStrings:Database" "Host=localhost;Port=5432;Database=traintrack;Username=traintrack;Password=traintrack_dev_only" --project src/backend/TrainTrack.Api

# narzędzia i migracje
dotnet tool restore
dotnet ef database update -p src/backend/TrainTrack.Infrastructure -s src/backend/TrainTrack.Api

# backend: build + testy (testy integracyjne startują własny PostgreSQL przez Testcontainers)
dotnet build && dotnet test

# API (http://localhost:5087) i frontend (http://localhost:5173) w osobnych terminalach
dotnet run --project src/backend/TrainTrack.Api
cd src/frontend && npm ci && npm run dev

# jakość frontendu
cd src/frontend && npm run lint && npm run typecheck && npm test

# E2E (Playwright sam startuje API i frontend, baza musi działać)
cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test
```

Jeśli zamiast JSON z `/health/ready` widzisz `404`, sprawdź, czy port 5087 nie jest zajęty przez stary proces API (`lsof -iTCP:5087`).

## Pierwsza sesja

```
Read CLAUDE.md, docs/development/status.md and docs/product/README.md.
Split docs/product/spec-traintrack.md into the files listed in docs/product/README.md.
Keep the content, do not invent requirements. Do not write application code.
Explain the split following docs/development/review-protocol.md
and wait for my acceptance before committing.
```

## Druga sesja – analityk

```
Read docs/product/. Act as a Senior Business Analyst.
Find gaps, contradictions and missing edge cases.
Ask me questions one at a time. Record answers in docs/product/business-rules.md.
Do not write code.
```

Potem: ADR-001…003, akceptacja `specs/foundation/00-walking-skeleton.md`
i slice 0 skillem `vertical-slice`.
