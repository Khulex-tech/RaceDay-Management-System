# RaceDay - Event Management System for South African Road Events

**Module:** PROG6212 - Programming 2B  
**Portfolio of Evidence - Part 1: System Planning and Database**

---

## 1. Project Overview

RaceDay is a web-based event management platform for the South African road running, walking and cycling community. South Africa hosts hundreds of road events every weekend — from the Comrades Marathon and the Cape Town Cycle Tour to community park runs and charity walks — yet many are still administered with paper entry forms, spreadsheets and scattered WhatsApp groups.

This Part 1 submission plans the full data model, API surface and SQL Server schema before any Part 2 API code is written.

---

## 2. System Roles

| Role | What the role can do |
| --- | --- |
| **Organiser** | Register and log in. Create, edit and delete their own events (name, description, date, location, distance, event type). Define categories per event (name, description, entry fee, age range). View enrolments for their events, confirm or cancel enrolments, and capture finish times and finishing positions. Upload an event banner (Part 3). |
| **Participant** | Register and log in. Browse and view events with categories. Enter an event by selecting a category. View and update their own enrolments (withdraw or change category before race day). View personal race history (finish time and finishing position). Update their profile and upload a profile picture (Part 3). |

Both roles live in one `Users` table as a `Role` column (`Organiser` or `Participant`). From Part 2 onward, the session stores `UserId` and `Role`; protected endpoints check role and ownership (Organiser → own events only; Participant → own enrolments/results only).

---

## 3. Repository structure (`/docs` deliverables)

```
.
├── .github/
│   └── workflows/
│       └── part1.yml                 CI workflow that validates Part 1 deliverables
├── docs/
│   ├── RACEDAYERD.png                Section A — ERD (PNG)
│   ├── API_EndpointPlan.md           Section B — API endpoint plan (Markdown)
│   └── RaceDay_DatabaseScript.sql    Section C — SQL Server schema + seed data
├── LICENSE
└── README.md
```

| Brief section | Requirement | File in this repo |
| --- | --- | --- |
| **Section A** | ERD as PNG or PDF in `/docs` | [`docs/RACEDAYERD.png`](docs/RACEDAYERD.png) |
| **Section B** | Endpoint plan as PDF or Markdown in `/docs` | [`docs/API_EndpointPlan.md`](docs/API_EndpointPlan.md) |
| **Section C** | `.sql` script in `/docs` | [`docs/RaceDay_DatabaseScript.sql`](docs/RaceDay_DatabaseScript.sql) |

---

## 4. Section A — Entity Relationship Diagram (ERD)

**Deliverable:** [`docs/RACEDAYERD.png`](docs/RACEDAYERD.png)

### Brief requirements checklist

| Requirement | How this submission meets it |
| --- | --- |
| Full RaceDay data model with a **minimum of six entities** | **Six entities:** `Users`, `EventTypes`, `Events`, `Categories`, `Enrolments`, `Results` |
| Show attributes, **primary keys**, **foreign keys**, and **cardinality** | Each entity lists attributes and types; PKs and FKs are labelled; crow's-foot notation shows one-to-many and the resolved many-to-many |
| Submit as **PNG or PDF** inside `/docs` | Submitted as PNG: `docs/RACEDAYERD.png` |
| SQL script in Section C must **match the ERD exactly** | See Section C and the alignment note below — **no deliberate differences** |

### Entities

| Entity | Purpose | Key attributes |
| --- | --- | --- |
| `Users` | Organisers and Participants | PK `UserId`; `Email` UNIQUE; `Role`; `PasswordHash`; optional `PhoneNumber`, `ProfilePictureUrl` |
| `EventTypes` | Run / Walk / Cycle | PK `EventTypeId`; `Name` UNIQUE |
| `Events` | Events owned by an Organiser | PK `EventId`; FK `OrganiserId` → `Users`; FK `EventTypeId` → `EventTypes` |
| `Categories` | Entry options for an event | PK `CategoryId`; FK `EventId` → `Events`; `EntryFee`, `MinAge`, `MaxAge` |
| `Enrolments` | Participant enters an event in a category | PK `EnrolmentId`; FKs `ParticipantId`, `EventId`, `CategoryId`; `EnrolmentStatus` |
| `Results` | Optional result for one enrolment | PK `ResultId`; FK `EnrolmentId` UNIQUE → `Enrolments` (enforces 1:1) |

### Relationships and cardinality

| Relationship | Cardinality | Notes |
| --- | --- | --- |
| `Users` (as Organiser) → `Events` | **One-to-many** | One Organiser organises many events |
| `EventTypes` → `Events` | **One-to-many** | One type has many events |
| `Events` → `Categories` | **One-to-many** | One event has many categories |
| `Users` (as Participant) ↔ `Events` | **Many-to-many** (via `Enrolments`) | Resolved by the `Enrolments` junction entity |
| `Categories` → `Enrolments` | **One-to-many** | One category is chosen by many enrolments |
| `Enrolments` → `Results` | **One-to-one** (optional) | An enrolment may have one result |

**Design notes**

1. **Many-to-many via `Enrolments`:** Participants and events cannot be linked directly. `Enrolments` stores `CategoryId` and `EnrolmentStatus` as well as the two FKs.
2. **`Results` hangs off `Enrolments`:** A result cannot exist without an enrolment; `UNIQUE(EnrolmentId)` enforces at most one result per enrolment.

---

## 5. Section B — API Endpoint Plan

**Deliverable:** [`docs/API_EndpointPlan.md`](docs/API_EndpointPlan.md)

### Brief requirements checklist

| Requirement | How this submission meets it |
| --- | --- |
| Table completed **before** Part 2 code | Plan is the Part 2 build contract; no API project code is in this repo yet |
| Columns: **HTTP Method, Route, Description, Role Required, Request Body, Expected Response** | Every row in `API_EndpointPlan.md` uses those six columns |
| Cover **Authentication** (register & login), **User Profile**, **Events**, **Categories**, **Event Enrolments**, **Results** | Sections 2–7 of the plan cover each required group (plus supporting endpoints) |
| Submit as **PDF or Markdown** in `/docs` | Submitted as Markdown: `docs/API_EndpointPlan.md` |
| Part 2 API must closely match this plan | Document purpose states Part 2 will be built against this plan |

### Resource groups planned

| Resource group | Covered in plan | Examples |
| --- | --- | --- |
| Authentication | Yes | `POST /api/auth/register`, `POST /api/auth/login` (+ logout / me) |
| User Profile | Yes | `GET/PUT /api/users/profile`, password change, profile picture |
| Events | Yes | List, detail, create, update, delete, banner upload, event types |
| Categories | Yes | Nested under events + category CRUD |
| Event Enrolments | Yes | Enter event, my enrolments, status/category updates, withdraw |
| Results | Yes | Capture, update, delete, event results, my results |

Role values used in the plan match the brief style: **None** (public), **Any** (logged in), or a specific role (**Organiser** / **Participant**). Routes are under `/api/`.

---

## 6. Section C — SQL Database Script

**Deliverable:** [`docs/RaceDay_DatabaseScript.sql`](docs/RaceDay_DatabaseScript.sql)

### Brief requirements checklist

| Requirement | How this submission meets it |
| --- | --- |
| `CREATE TABLE` for **every entity in the ERD** | Creates all six tables: `Users`, `EventTypes`, `Events`, `Categories`, `Enrolments`, `Results` |
| **Primary keys, foreign keys**, and constraints (`NOT NULL`, `UNIQUE`, `DEFAULT`) | Defined on every table (plus `CHECK` constraints for role, status, distance, ages, position) |
| Seed data: **≥ 2 Organisers**, **≥ 2 Participants**, **≥ 3 Events**, categories per event, sample enrolments | **2 Organisers, 4 Participants, 3 Events, 10 categories, 8 enrolments, 3 results** |
| Saved as `.sql` in `/docs` | `docs/RaceDay_DatabaseScript.sql` |
| Runs without errors on a **clean SQL Server** instance | Script drops/recreates `RaceDayDb`, then creates schema and seeds data |

### Sample data summary

| Seed item | Count | Detail |
| --- | --- | --- |
| Organisers | 2 | Thabo Mokoena, Ayesha Patel |
| Participants | 4 | Sipho, Lerato, Johan, Nomvula |
| Events | 3 | Soweto Marathon (Run), Cape Town Cycle Tour (Cycle), Durban Beachfront Charity Walk (Walk) |
| Categories | 10 | Multiple categories on each event |
| Enrolments | 8 | Statuses include Pending, Confirmed and Cancelled |
| Results | 3 | Finish times and finishing positions for the completed marathon |

### How to run (SSMS)

1. Open `docs/RaceDay_DatabaseScript.sql` in SQL Server Management Studio.
2. Connect to a clean SQL Server instance.
3. Execute the full script (F5).
4. Review the verification `SELECT` statements at the end (row counts, enrolments, results).

Or from the command line:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "docs/RaceDay_DatabaseScript.sql"
```

---

## 7. ERD and SQL alignment (Section A ↔ Section C)

Per the brief: *“Your SQL script in Section C must match your ERD exactly. Any deliberate differences must be explained in the README.”*

**There are no deliberate differences.** The script matches `RACEDAYERD.png` for:

- the same six entity names;
- the same attributes and data types;
- the same primary keys and foreign keys;
- the same cardinalities (including the optional 1:1 `Enrolments` ↔ `Results` via `UNIQUE EnrolmentId`).

---

## 8. CI/CD

GitHub Actions runs [`.github/workflows/part1.yml`](.github/workflows/part1.yml) on every push to `main`/`master` and on pull requests. The workflow checks that:

- `docs/` and `.github/workflows/` exist;
- the ERD PNG (`docs/RACEDAYERD.png`), endpoint plan (`docs/API_EndpointPlan.md`), SQL script (`docs/RaceDay_DatabaseScript.sql`) and `README.md` are present;
- the endpoint plan includes the six required columns and covers Authentication, User Profile, Events, Categories, Enrolments and Results;
- the SQL script creates all six ERD tables and includes primary keys, foreign keys, constraints and seed `INSERT`s;
- the README documents the Project Overview, System Roles and CI/CD.

A green Part 1 build means the repository structure and deliverables match what this workflow expects.

---

## 9. Next steps

| Part | Focus |
| --- | --- |
| **Part 2** | ASP.NET Core Web API implementing `API_EndpointPlan.md` against `RaceDayDb` |
| **Part 3** | MVC front end, file uploads (profile picture and event banner), and Docker packaging |

---

## 10. Video presentation

**YouTube (unlisted):** _<add your unlisted YouTube link here>_

The video walks through the ERD (entities, keys, cardinality), the endpoint plan (routes, roles, responses), and a live run of the SQL script in SSMS ending with the check queries.

---

## 11. Author

**Name:** Thapelo Mkhari (Khulex-tech)  
**Module:** PROG6212 - Programming 2B  
**Part:** 1 of 3 - System Planning and Database
