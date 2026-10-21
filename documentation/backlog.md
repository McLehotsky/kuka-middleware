# Backlog – KUKA FM Middleware

> Source of truth: wiki *ITS – Overview* (21. 10. 2026) – požiadavky, use casy UC1–UC7, diagramy 4.0–4.5, ERD a mockupy 6.1–6.5. Štruktúra podľa referenčného repozitára [FEI-thridos/ITS](https://github.com/FEI-thridos/ITS): Epic → Features → User Stories (= use casy) → Tasks po vrstvách. Import do Azure Boards: [backlog.csv](backlog.csv).

**Rozsah:** 1 Epic · 4 Features · 12 User Stories · 38 Tasks · odhad **128 h**

## Súhrn

| # | Feature | Požiadavky | User Stories | Odhad |
|---|---|---|---|---|
| 1 | Správa misií | FR1; FR2; FR5; FR6 | UC1, UC2, UC3, UC4 | 52 h |
| 2 | Prehľad robotov | FR7 | UC6 | 9 h |
| 3 | Synchronizácia s KUKA FM | FR3; FR4; FR5; NFR3 | Prístup ku KUKA FM API s access tokenom, UC7, UC5 | 29 h |
| 4 | Nasadenie a prevádzka | NFR1; NFR2; NFR4; NFR5 | Kontajnerizácia a databáza, Zabezpečenie API a logovanie, CI pipeline, CD pipeline | 38 h |
| | **Spolu** | | | **128 h** |

**Epic – akceptačné kritériá (wiki 8):**

- [ ] Je možné zadať novú misiu.
- [ ] Je možné zrušiť prebiehajúcu misiu.
- [ ] Je možné prezrieť stav prebiehajúcich misií, ktoré sa aktualizujú v reálnom čase, a ukončených misií.
- [ ] Je možné prezrieť stav robotov, ktoré sa aktualizujú v reálnom čase.
- [ ] Prístup ku KUKA FM API volaniu je zabezpečený access tokenom.
- [ ] Všetky operácie vykonané používateľom sú logované.
- [ ] Proces testovania a nasadenia je plne automatizovaný využitím CI/CD pipeline.

## 1. Správa misií *(FR1; FR2; FR5; FR6)*

Zadanie, zrušenie a prehľad misií vrátane histórie stavov (UC1–UC4, wiki 3.1).

- Misie sa dajú zadať, zrušiť a sledovať cez middleware bez použitia UI KUKA FM.
- Každá misia má aktuálny stav aj celú históriu zmien.

### 1.1 UC1 – Zadať misiu — 19 h

*Ako operátor / externý systém chcem zadať novú misiu robotovi, aby ju KUKA FM vykonal.*

**Akceptačné kritériá**

- Formulár / POST /api/missions prijme typ misie, cieľové body a voliteľne robota.
- Neplatný vstup vráti 400, neautorizovaný request 401; KUKA FM sa nevolá.
- Po úspešnom submitMission je misia uložená so stavom Created/Submitted, má prvý záznam v histórii a aktér dostane jej ID.
- Ak KUKA FM misiu odmietne alebo je nedostupné, misia je Failed, chyba sa zaloguje a API vráti 502.

| Task | Activity | h |
|---|---|---|
| [BE] Endpoint POST /api/missions + validácia vstupu | Development | 4 |
| [BE] Volanie submitMission + uloženie misie a histórie | Development | 5 |
| [FE] Obrazovka Nová misia (mockup 6.2) | Development | 6 |
| [TEST] Unit testy zadania misie | Testing | 4 |

### 1.2 UC2 – Zrušiť misiu — 12 h

*Ako operátor / externý systém chcem zrušiť rozbehnutú misiu.*

**Akceptačné kritériá**

- Zrušiť je možné len misiu, ktorá nie je ukončená; v UI vyžaduje potvrdenie.
- Neexistujúca misia vráti 404, ukončená 409.
- Po úspešnom cancelMission je misia v stave Cancelling a zmena je v histórii.
- Finálny stav Cancelled potvrdí synchronizácia (UC5).

| Task | Activity | h |
|---|---|---|
| [BE] Endpoint POST /api/missions/{id}/cancel + kontroly 404/409 | Development | 3 |
| [BE] Volanie cancelMission + stav Cancelling | Development | 3 |
| [FE] Tlačidlo Zrušiť s potvrdením (mockup 6.4) | Development | 3 |
| [TEST] Unit testy zrušenia misie | Testing | 3 |

### 1.3 UC3 – Zobraziť zoznam misií — 11 h

*Ako operátor / externý systém chcem vidieť prebiehajúce aj ukončené misie s aktuálnym stavom.*

**Akceptačné kritériá**

- Zoznam zobrazuje misie s aktuálnym stavom, voliteľne filter aktívne / ukončené.
- Dáta sa čítajú len z vlastnej DB, KUKA FM sa nevolá.
- UI sa priebežne obnovuje (real-time aktualizácia).

| Task | Activity | h |
|---|---|---|
| [BE] Endpoint GET /api/missions?state=active|finished | Development | 3 |
| [FE] Obrazovka Zoznam misií + filter + auto-refresh (mockup 6.1) | Development | 6 |
| [TEST] Unit testy filtra aktívne / ukončené | Testing | 2 |

### 1.4 UC4 – Zobraziť históriu stavov misie — 10 h

*Ako operátor chcem vidieť časovú os zmien stavu konkrétnej misie.*

**Akceptačné kritériá**

- Z detailu misie je vidieť všetky záznamy histórie stavov zoradené podľa času.
- Neexistujúca misia vráti 404.

| Task | Activity | h |
|---|---|---|
| [BE] Endpointy GET /api/missions/{id} a /api/missions/{id}/history | Development | 3 |
| [FE] Obrazovka Detail misie s časovou osou (mockup 6.3) | Development | 5 |
| [TEST] Unit testy histórie (poradie, 404) | Testing | 2 |

## 2. Prehľad robotov *(FR7)*

Zoznam robotov a ich aktuálneho stavu (UC6, wiki 3.2).

- UI zobrazuje všetky roboty so stavom Dostupný / Zaneprázdnený / Offline.

### 2.1 UC6 – Zobraziť zoznam robotov — 9 h

*Ako operátor chcem vidieť roboty a ich aktuálny stav.*

**Akceptačné kritériá**

- Zoznam zobrazuje roboty a ich stav (dostupný / zaneprázdnený / offline).
- Dáta sa čítajú len z vlastnej DB a UI sa priebežne obnovuje.

| Task | Activity | h |
|---|---|---|
| [BE] Endpoint GET /api/robots | Development | 2 |
| [FE] Obrazovka Roboty + auto-refresh (mockup 6.5) | Development | 5 |
| [TEST] Unit testy endpointu robotov | Testing | 2 |

## 3. Synchronizácia s KUKA FM *(FR3; FR4; FR5; NFR3)*

Prístup ku KUKA FM s tokenom a periodická synchronizácia stavov misií a robotov (UC5, UC7, wiki 3.3 a 4.0).

- Stavy misií a robotov v DB sú aktuálne bez zásahu používateľa.
- Každé volanie na KUKA FM obsahuje platný access token.

### 3.1 Prístup ku KUKA FM API s access tokenom — 12 h

*Ako middleware potrebujem pri každom volaní na KUKA FM platný token (diagram 4.0).*

**Akceptačné kritériá**

- Všetky volania (submitMission, cancelMission, Query jobs, query robotov) idú cez jedného klienta.
- Pred volaním sa overí token v cache; ak nie je platný, vydá sa nový a uloží do cache.
- Každé volanie má hlavičku Authorization.
- Pri odpovedi 401 sa token zneplatní a obnoví najviac raz.
- Úspešné aj neúspešné volania a chyba autentifikácie sa zalogujú.

| Task | Activity | h |
|---|---|---|
| [BE] Typovaný klient KUKA FM (submitMission, cancelMission, Query jobs, query robotov) | Development | 5 |
| [BE] Token – vydanie, cache, hlavička Authorization, obnova pri 401 | Development | 4 |
| [TEST] Unit testy klienta a tokenu | Testing | 3 |

### 3.2 UC7 – Synchronizovať stav robotov — 8 h

*Ako plánovač chcem periodicky načítať stav robotov z KUKA FM.*

**Akceptačné kritériá**

- Plánovač v intervale zavolá query API robotov na KUKA FM.
- Stav KUKA FM sa namapuje na dostupný / zaneprázdnený / offline.
- Nové roboty sa vložia, existujúcim sa aktualizuje stav a LastSeenAt.
- Pri neúspešnom volaní sa chyba zaloguje a dáta sa nemenia.

| Task | Activity | h |
|---|---|---|
| [BE] Background služba – query robotov, mapovanie stavov, vloženie / aktualizácia | Development | 5 |
| [TEST] Unit testy synchronizácie robotov | Testing | 3 |

### 3.3 UC5 – Synchronizovať stav misií — 9 h

*Ako plánovač chcem periodicky aktualizovať stav aktívnych misií z KUKA FM.*

**Akceptačné kritériá**

- Plánovač v intervale (napr. 5 s) načíta aktívne misie (Submitted, InProgress, Cancelling) a zavolá Query jobs API.
- Pri zmene stavu sa aktualizuje Mission.Status a pridá sa záznam do histórie.
- Koncový stav (Completed, Cancelled, Failed) nastaví FinishedAt.
- Každej spracovanej misii sa nastaví LastSyncedAt a zmeny sa uložia v transakcii.
- Ak je KUKA FM nedostupné, chyba sa zaloguje a ďalší pokus prebehne v nasledujúcom cykle.

| Task | Activity | h |
|---|---|---|
| [BE] Background služba – Query jobs, porovnanie stavov, zápis histórie | Development | 6 |
| [TEST] Unit testy synchronizácie misií | Testing | 3 |

## 4. Nasadenie a prevádzka *(NFR1; NFR2; NFR4; NFR5)*

Kontajnerizácia, databáza, zabezpečenie API, logovanie a CI/CD.

- Aplikácia beží v Dockeri a je zbuildovaná, otestovaná a nasadená pipeline bez ručných krokov.

### 4.1 Kontajnerizácia a databáza — 14 h

*Ako tím chceme spustiť celé riešenie jedným príkazom s perzistentnou DB.*

**Akceptačné kritériá**

- docker compose up spustí web, api a PostgreSQL.
- Schéma DB zodpovedá ERD (wiki 5) a vzniká EF Core migráciou pri štarte API.
- Dáta prežijú reštart kontajnerov.

| Task | Activity | h |
|---|---|---|
| [BE] Kostra .NET riešenia + EF Core DbContext a migrácia podľa ERD | Development | 5 |
| [FE] Kostra Vue 3 projektu (Vite, Vue Router) | Development | 2 |
| [DevOps] Dockerfile pre API a web (nginx, proxy /api) | Deployment | 4 |
| [DevOps] docker-compose.yml (web, api, postgres s volume) + .env.example | Deployment | 3 |

### 4.2 Zabezpečenie API a logovanie — 11 h

*Ako prevádzkovateľ chcem, aby API prijímalo len autorizované requesty a všetky operácie boli zalogované.*

**Akceptačné kritériá**

- Request bez platného API kľúča dostane 401.
- Middleware zapisuje štruktúrované logy o vlastnej činnosti a chybách na stdout.
- Každé zadanie a zrušenie misie (aj neúspešné) má záznam v AuditLog.

| Task | Activity | h |
|---|---|---|
| [BE] Autentifikácia API kľúčom (ApiClient, hash v DB, seed pre web UI a externý systém) | Development | 4 |
| [BE] Štruktúrované logy na stdout | Development | 2 |
| [BE] Audit operácií do AuditLog (UC1, UC2) | Development | 3 |
| [TEST] Testy API kľúča a auditu | Testing | 2 |

### 4.3 CI pipeline — 6 h

*Ako tím chceme, aby sa každá zmena automaticky zbuildovala a otestovala.*

**Akceptačné kritériá**

- Pipeline zbuilduje backend aj frontend a spustí testy.
- Prebehne statická analýza; pri chybe buildu, testov alebo analýzy pipeline zlyhá.
- PR do main nejde zmergovať bez úspešnej pipeline.

| Task | Activity | h |
|---|---|---|
| [DevOps] Azure Pipeline – build, testy, publikovanie výsledkov | Deployment | 3 |
| [DevOps] Statická analýza backendu a frontendu | Deployment | 2 |
| [DevOps] Branch policy na main s povinnou pipeline | Deployment | 1 |

### 4.4 CD pipeline — 7 h

*Ako tím chceme, aby sa nová verzia nasadila automaticky.*

**Akceptačné kritériá**

- Po merge do main sa aplikácia nasadí bez ručného zásahu.
- Tajné hodnoty (KUKA token, DB heslo, API kľúče) sú v secret premenných, nie v repozitári.
- Po nasadení prebehne kontrola web aj /api/health, inak pipeline zlyhá.

| Task | Activity | h |
|---|---|---|
| [DevOps] Deploy stage – build image + docker compose up | Deployment | 3 |
| [DevOps] Secrets cez variable group | Deployment | 2 |
| [BE] Health endpoint /api/health + smoke test po nasadení | Deployment | 2 |
