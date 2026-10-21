# ITS – Middleware pre KUKA AMR Fleet Manager

  

> Zdroj: *ITS - Overview* (Azure DevOps wiki, posledná úprava 21. 10. 2026). Tento dokument rozpracúva zadanie bod po bode, diagramy sú v [Mermaid](https://mermaid.js.org/).

  

**Stav rozpracovania**

  

| Sekcia | Stav |
|---|---|
| 1. Ciele a stav | prepísané |
| 2. Požiadavky | prepísané |
| 3. Prípady použitia | hotové |
| 4. Diagramy aktivít | hotové |
| 5. Dátový model (ERD) | návrh |
| 6. Návrh view aplikácie | TODO |
| 7. Technológie | prepísané |
| 8. Akceptačné kritériá | prepísané |

  

---

  

## 1. Ciele a stav

  

- **Súčasný stav:** KUKA AMR Fleet Manager (KUKA FM) beží samostatne, bez napojenia na okolité systémy (ERP/PLM/WMS) — obsluha musí misie zadávať/sledovať priamo v jeho UI.

- **Cieľový stav:** middleware sprostredkúva riadenie a sledovanie misií pre iný (fiktívny) systém — zjednodušuje a zjednocuje prístup k funkciám KUKA FM cez vlastné REST rozhranie a webové UI.

  

::: mermaid

flowchart LR

    subgraph AS_IS["Súčasný stav"]

        OP1["Obsluha"] --> FM1["KUKA FM UI"]

        ERP1["ERP / WMS / PLM"] -.-x|bez napojenia| FM1

    end

    subgraph TO_BE["Cieľový stav"]

        OP2["Obsluha"] --> WEB["Web UI middlewaru"]

        ERP2["Externý systém"] -->|REST| API["Middleware API"]

        WEB --> API

        API --> DB[("PostgreSQL")]

        API -->|REST + token| FM2["KUKA FM"]

    end

:::

  

---

  

## 2. Požiadavky

  

### Funkcionálne požiadavky (FR)

  

| ID | Požiadavka |
|---|---|
| FR1 | Middleware umožní zadať (dispatch) novú misiu robotovi cez vlastný endpoint, ktorý interne zavolá `submitMission` na KUKA FM. |
| FR2 | Middleware umožní zrušiť (cancel) rozbehnutú misiu cez vlastný endpoint, ktorý interne zavolá `cancelMission` na KUKA FM. |
| FR3 | Middleware periodicky (pollingom) zisťuje aktuálny stav aktívnych misií cez Query jobs API KUKA FM a ukladá ho do vlastnej databázy. |
| FR4 | Middleware periodicky zisťuje aktuálny stav robotov (dostupný/zaneprázdnený/offline) cez príslušné query API KUKA FM a ukladá ho do vlastnej databázy. |
| FR5 | Middleware uchováva históriu stavov jednotlivých misií (nie len posledný stav) pre základné reportovanie. |
| FR6 | Middleware poskytuje webové rozhranie (frontend) so zoznamom misií a ich aktuálnym/historickým stavom. |
| FR7 | Middleware poskytuje webové rozhranie so zoznamom robotov a ich aktuálnym stavom. |

  

### Nefunkcionálne požiadavky (NFR)

  

| ID | Požiadavka |
|---|---|
| NFR1 | Middleware je kontajnerizovaný a používa perzistentnú databázu. |
| NFR2 | Vlastné API middlewaru je chránené — neautorizovaný/neplatný request je odmietnutý. |
| NFR3 | Middleware overuje platnosť KUKA FM `Authorization` tokenu pri každom volaní na KUKA FM (nutné od úplného začiatku vývoja, inak žiadne volanie nefunguje). |
| NFR4 | Middleware zapisuje štruktúrované logy o vlastnej činnosti a chybách. |
| NFR5 | Middleware je nasaditeľný cez CI/CD pipeline (build, test, statická analýza, redeploy). |

---

## 3. Prípady použitia (use-case diagram)

> Zadanie: diagram + krátky textový popis pre každý use case (aktér, cieľ, hlavný scenár) — správa misií (vrátane ukončených), správa robotov.

### Aktéri

| Aktér | Typ | Popis |
|---|---|---|
| Operátor | primárny (človek) | Pracuje s webovým UI middlewaru. |
| Externý systém | primárny (systém) | Fiktívny ERP/WMS, volá REST API middlewaru. |
| Plánovač (polling) | primárny (časovač) | Interná služba middlewaru, periodicky spúšťa synchronizáciu. |
| KUKA FM | sekundárny (systém) | Vykonáva misie, poskytuje stav misií a robotov. |

### 3.1 Správa misií *(FR1, FR2, FR5, FR6)*

![01-sprava-misii.svg](/.attachments/01-sprava-misii-980e527e-9650-42a0-b355-3a37e99ee0f1.svg)
  
### 3.2 Prehľad robotov *(FR7)*
![02-prehlad-robotov.png](/.attachments/02-prehlad-robotov-df871351-cc33-4473-95fd-230c4c6e7581.png)

### 3.3 Synchronizácia údajov s KUKA FM *(FR3, FR4, FR5)*
![03-synchronizacia.png](/.attachments/03-synchronizacia-ad4ba4eb-a44d-4210-b824-0711aecec231.png)

### Popis use casov

  

#### UC1 – Zadať misiu *(FR1)*

- **Aktér:** Operátor, Externý systém; sekundárny KUKA FM

- **Cieľ:** Odoslať novú misiu robotovi.

- **Hlavný scenár:**

  1. Aktér zadá parametre misie (typ, cieľové body, voliteľne robot).

  2. Middleware overí autorizáciu requestu a validuje vstup.

  3. Middleware zavolá `submitMission` na KUKA FM s platným tokenom.

  4. Middleware uloží misiu do DB so stavom *Created/Submitted* a prvý záznam do histórie.

  5. Aktér dostane ID misie.

- **Alternatívy:** neplatný vstup → 400; neautorizovaný → 401; KUKA FM odmietne/nedostupné → chyba sa zaloguje a vráti 502.

  

#### UC2 – Zrušiť misiu *(FR2)*

- **Aktér:** Operátor, Externý systém; sekundárny KUKA FM

- **Cieľ:** Zastaviť rozbehnutú misiu.

- **Hlavný scenár:**

  1. Aktér vyberie aktívnu misiu a zvolí *Zrušiť*.

  2. Middleware overí, že misia existuje a nie je ukončená.

  3. Middleware zavolá `cancelMission` na KUKA FM.

  4. Middleware zapíše zmenu stavu (*Cancelling/Cancelled*) do histórie.

- **Alternatívy:** misia už ukončená → 409; misia neexistuje → 404.

  

#### UC3 – Zobraziť zoznam misií *(FR6)*

- **Aktér:** Operátor, Externý systém

- **Cieľ:** Prehľad prebiehajúcich aj ukončených misií s aktuálnym stavom.

- **Hlavný scenár:**

  1. Aktér otvorí zoznam misií (voliteľne filter aktívne / ukončené).

  2. Middleware vráti misie z vlastnej DB (nevolá KUKA FM).

  3. UI sa priebežne obnovuje (real-time aktualizácia).

  

#### UC4 – Zobraziť históriu stavov misie *(FR5, FR6)*

- **Aktér:** Operátor

- **Cieľ:** Vidieť časovú os zmien stavu konkrétnej misie.

- **Hlavný scenár:**

  1. Operátor v zozname misií otvorí detail misie.

  2. Middleware vráti záznamy histórie stavov zoradené podľa času.

  

#### UC5 – Synchronizovať stav misií *(FR3, FR5)*

- **Aktér:** Plánovač; sekundárny KUKA FM

- **Cieľ:** Udržiavať stav aktívnych misií v DB aktuálny.

- **Hlavný scenár:**

  1. Plánovač v intervale (napr. 5 s) spustí synchronizáciu.

  2. Middleware zavolá Query jobs API KUKA FM pre aktívne misie.

  3. Pri zmene stavu aktualizuje misiu a pridá záznam do histórie.

- **Alternatívy:** KUKA FM nedostupné → zaloguje sa chyba, ďalší pokus v nasledujúcom cykle.

  

#### UC6 – Zobraziť zoznam robotov *(FR7)*

- **Aktér:** Operátor

- **Cieľ:** Prehľad robotov a ich aktuálneho stavu (dostupný / zaneprázdnený / offline).

- **Hlavný scenár:**

  1. Operátor otvorí obrazovku robotov.

  2. Middleware vráti roboty z DB, UI sa priebežne obnovuje.

  

#### UC7 – Synchronizovať stav robotov *(FR4)*

- **Aktér:** Plánovač; sekundárny KUKA FM

- **Cieľ:** Udržiavať stav robotov v DB aktuálny.

- **Hlavný scenár:**

  1. Plánovač v intervale spustí synchronizáciu.

  2. Middleware zavolá query API robotov na KUKA FM.

  3. Nové roboty vloží, existujúcim aktualizuje stav a čas poslednej aktualizácie.

  

---

  

## 4. Procesný diagram / diagram aktivít

  

> Zadanie: diagramy zachytávajú procesný tok pre jednotlivé prípady použitia.

  

### 4.0 Spoločný podproces – volanie KUKA FM s overením tokenu *(NFR3, NFR4)*

  

Každé volanie na KUKA FM (UC1, UC2, UC5, UC7) prechádza týmto podprocesom.

  

![4.0-kuka-token.svg](/.attachments/4.0-kuka-token-a722b549-e5a1-45a9-b3b3-b1c8499bbfe7.svg)

  

> Pozn.: pri opakovanej odpovedi 401 sa token obnoví najviac raz, aby nevznikol nekonečný cyklus.

  

### 4.1 UC1 – Zadať misiu

  
![4.1-uc01-zadat-misiu.svg](/.attachments/4.1-uc01-zadat-misiu-076b51ee-e44c-461d-94d4-30fcbde1121e.svg)

  

### 4.2 UC2 – Zrušiť misiu

  
![4.2-uc02-zrusit-misiu.svg](/.attachments/4.2-uc02-zrusit-misiu-f4bb03f3-dd12-4cb9-8b33-8a0812a9d6dd.svg)

  

> Pozn.: finálny stav *Cancelled* potvrdí až synchronizácia (UC5), keď ho KUKA FM vráti.

  

### 4.3 UC3 / UC4 / UC6 – Zobrazenie misií, histórie a robotov

  

Čítanie ide výhradne z vlastnej DB, KUKA FM sa nevolá. Real-time aktualizácia je riešená periodickým obnovovaním UI (alternatívne SignalR push).

  
![4.3-uc03-uc06-prezeranie.svg](/.attachments/4.3-uc03-uc06-prezeranie-b4087002-d89b-4312-abba-89c449065335.svg)

  

### 4.4 UC5 – Synchronizovať stav misií

  

![4.4-uc07-sync-misii.svg](/.attachments/4.4-uc07-sync-misii-2c319451-e65f-4b51-aae3-f3123ff369d8.svg)

  

### 4.5 UC7 – Synchronizovať stav robotov

  

![4.5-uc08-sync-robotov.svg](/.attachments/4.5-uc08-sync-robotov-618d0066-8666-4c2f-9948-3f78d668d86d.svg)
  

---

  

## 5. Dátový model (ERD)

  

> Zadanie: entity, ktoré middleware ukladá vo vlastnej DB (napr. `Mission`, `Robot`, `StatusHistory`) a vzťahy medzi nimi.

  

### Rozhodnutia pre model

  

- **Autorizácia:** API kľúč per klient (`ApiClient`) — jeden kľúč pre web UI operátora, jeden pre externý systém. Ak sa neskôr zavedie prihlasovanie operátorov, `ApiClient` sa nahradí / doplní entitou `User` a FK v `Mission` a `AuditLog` sa presmerujú.

- **Audit vs. technické logy:** operácie používateľa (zadanie, zrušenie misie) sa ukladajú do tabuľky `AuditLog` (akceptačné kritérium *„všetky operácie vykonané používateľom sú logované"*). Technické štruktúrované logy (NFR4) idú na stdout / log sink, nie do DB.

- **História stavov:** len pre misie (FR5). História robotov nie je požadovaná, ukladá sa iba aktuálny stav (FR4).

  

### Diagram

  

::: mermaid

erDiagram

    ApiClient ||--o{ Mission : "zadal"

    ApiClient ||--o{ AuditLog : "vykonal"

    Robot |o--o{ Mission : "vykonáva"

    Mission ||--|{ MissionStep : "obsahuje"

    Mission ||--|{ MissionStatusHistory : "má históriu"

    Mission |o--o{ AuditLog : "týka sa"

  

    ApiClient {

        int Id PK

        string Name

        string KeyHash

        string Role

        bool IsActive

        timestamptz CreatedAt

    }

  

    Robot {

        int Id PK

        string KukaRobotId

        string Name

        string Status

        int BatteryLevel

        string CurrentNode

        timestamptz LastSeenAt

        timestamptz UpdatedAt

    }

  

    Mission {

        uuid Id PK

        string MissionCode

        string ExternalRef

        string MissionType

        int Priority

        string Status

        int RobotId FK

        int CreatedByClientId FK

        timestamptz CreatedAt

        timestamptz SubmittedAt

        timestamptz FinishedAt

        timestamptz LastSyncedAt

        string ErrorMessage

    }

  

    MissionStep {

        int Id PK

        uuid MissionId FK

        int Sequence

        string NodeCode

        string Action

    }

  

    MissionStatusHistory {

        bigint Id PK

        uuid MissionId FK

        string PreviousStatus

        string Status

        string Source

        string Message

        timestamptz ChangedAt

    }

  

    AuditLog {

        bigint Id PK

        int ApiClientId FK

        uuid MissionId FK

        string Action

        bool Success

        int HttpStatus

        jsonb Details

        string CorrelationId

        timestamptz Timestamp

    }

:::

  

### Popis entít

  

| Entita | Účel | Plnená z | Požiadavka |
|---|---|---|---|
| `ApiClient` | Klient oprávnený volať API middlewaru. Ukladá sa len hash kľúča. | seed / admin | NFR2 |
| `Robot` | Aktuálny stav robota. | UC7 (polling KUKA FM) | FR4, FR7 |
| `Mission` | Misia a jej posledný známy stav. | UC1, UC2, UC5 | FR1, FR2, FR3, FR6 |
| `MissionStep` | Poradie cieľových bodov a akcií misie. | UC1 | FR1 |
| `MissionStatusHistory` | Každá zmena stavu misie (append-only). | UC1, UC2, UC5 | FR5, FR6 |
| `AuditLog` | Operácie vykonané klientom vrátane neúspešných. | UC1, UC2 | akceptačné kritérium |

### Stavy misie

::: mermaid

stateDiagram-v2

    [*] --> Created : POST /api/missions

    Created --> Submitted : submitMission OK

    Created --> Failed : submitMission chyba

    Submitted --> InProgress : sync – robot začal

    Submitted --> Cancelling : cancel

    InProgress --> Cancelling : cancel

    InProgress --> Completed : sync – dokončené

    InProgress --> Failed : sync – chyba

    Cancelling --> Cancelled : sync – potvrdené

    Completed --> [*]

    Cancelled --> [*]

    Failed --> [*]

:::

  

- **Aktívne** (synchronizované v UC5): `Submitted`, `InProgress`, `Cancelling`

- **Koncové** (nastaví sa `FinishedAt`): `Completed`, `Cancelled`, `Failed`

  

> Pozn.: presné mapovanie na stavy z Query jobs API KUKA FM sa doplní podľa dokumentácie KUKA FM.

  

### Indexy a obmedzenia

  

| Tabuľka | Index / obmedzenie | Dôvod |
|---|---|---|
| `Mission` | `UNIQUE (MissionCode)` | párovanie s KUKA FM |
| `Mission` | `(Status)` | UC5 vyberá aktívne misie, UI filtruje aktívne/ukončené |
| `Mission` | `(CreatedAt DESC)` | zoznam misií v UI |
| `MissionStatusHistory` | `(MissionId, ChangedAt)` | časová os misie (UC4) |
| `Robot` | `UNIQUE (KukaRobotId)` | upsert pri synchronizácii (UC7) |
| `ApiClient` | `UNIQUE (KeyHash)` | vyhľadanie klienta pri autorizácii |
| `AuditLog` | `(Timestamp DESC)`, `(MissionId)` | prehľad operácií |

  

Enum hodnoty (`Status`, `Role`, `Action`, …) sa v EF Core ukladajú ako `string` (`HasConversion<string>()`) kvôli čitateľnosti v DB.

  

---

  

## 6. Návrh view aplikácie

  

> Zadanie: wireframe/mockup webového rozhrania — obrazovka pre manažment misií, obrazovka so zoznamom misií a ich stavom, obrazovka so zoznamom robotov a ich stavom. Stačí jednoduchý návrh, nie hotový grafický dizajn.

### 6.1 Zoznam misii
![6.1-zoznam-misii.svg](/.attachments/6.1-zoznam-misii-9d125cca-6b91-4f2e-a47c-3cf9d0650a01.svg)

### 6.2 Nova misia
![6.2-nova-misia.svg](/.attachments/6.2-nova-misia-2fe6cb11-6aa2-4762-bab3-dec1ec068e34.svg)
### 6.3 Detail misie
![6.3-detail-misie.svg](/.attachments/6.3-detail-misie-59ddd33b-683a-441c-87e0-9fa032593390.svg)
### 6.4 Zrusenie misie
![6.4-zrusenie-misie.svg](/.attachments/6.4-zrusenie-misie-6448b17f-7dd4-4f7d-9ad1-c2cde430f8da.svg)
### 6.5
![6.5-roboty.svg](/.attachments/6.5-roboty-7ba77fbe-b683-4b3e-9e47-b22cb65d574d.svg)
  

---

  

## 7. Technológie

  

Použité technológie pri vývoji middlewaru, napr.:

  

- **Backend:** ASP.NET Core minimal API (C#), Entity Framework Core

- **DB:** PostgreSQL

- **Frontend:** Vue3 Framework

- **Kontajnerizácia:** Docker + docker-compose

  

---

  

## 8. Akceptačné kritériá

  

- [ ] Je možné zadať novú misiu.

- [ ] Je možné zrušiť prebiehajúcu misiu.

- [ ] Je možné prezrieť stav prebiehajúcich misií, ktoré sa aktualizujú v reálnom čase, a ukončených misií.

- [ ] Je možné prezrieť stav robotov, ktoré sa aktualizujú v reálnom čase.

- [ ] Prístup ku KUKA FM API volaniu je zabezpečený access tokenom.

- [ ] Všetky operácie vykonané používateľom sú logované.

- [ ] Proces testovania a nasadenia je plne automatizovaný využitím CI/CD pipeline.