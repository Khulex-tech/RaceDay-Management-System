# RaceDay - Event Management System for South African Road Events

**Module:** PROG6212 - Programming 2B  
**Portfolio of Evidence - Part 1: System Planning and Database**

---

## 1. System description

RaceDay is a web-based event management platform for the South African road running, walking and cycling community. South Africa hosts hundreds of road events every weekend - from the Comrades Marathon and the Cape Town Cycle Tour to community park runs and charity walks - yet many are still administered with paper entry forms, spreadsheets and scattered WhatsApp groups.

This Part 1 submission covers the planned data model, API surface and database script that Parts 2 and 3 will implement against.

---

## 2. The two user roles

| Role | What the role can do |
| --- | --- |
| **Organiser** | Register and log in as an Organiser. Create, edit and delete their own events, capturing name, description, date, location, distance and event type (Run, Walk or Cycle). Define categories per event with name, description, entry fee and age range. View every enrolment for their own events, including the Participant's selected category and enrolment status. Confirm or cancel enrolments. Capture finish times and finishing positions after the event. Upload an event banner image (Part 3). |
| **Participant** | Register and log in as a Participant. Browse and filter upcoming events and view full event detail with the available categories. Enter an event by selecting a category, which records the Participant–event–category link immediately. View all of their own enrolments and each enrolment's status. Withdraw from an event or change category before race day. View their personal race history — event, date, category, finish time and finishing position. View and update their own profile and upload a profile picture (Part 3). |

Both roles are stored as a `Role` column on a single `Users` table (`Organiser` or `Participant`), so registration and login work the same way for each. From Part 2 onward, the session stores the user's `UserId` and `Role`, every protected endpoint checks the role, and ownership is checked on top of the role — an Organiser can only manage events they created, and a Participant can only see their own enrolments and results. Part 3 reflects the same separation in the MVC interface with a different navigation menu per role.

---

## 3. Database model (six tables)

| Table | Purpose |
| --- | --- |
| `Users` | Organisers and Participants (role stored as a column) |
| `EventTypes` | Fixed types: Run, Walk, Cycle |
| `Events` | Events created by Organisers |
| `Categories` | Age-bounded entry options per event, including entry fee |
| `Enrolments` | Participant–event–category link with status (`Pending`, `Confirmed`, `Cancelled`) |
| `Results` | Optional 1:1 finish time and finishing position per enrolment |

---

## 4. Repository structure

```
.
├── docs/
│   ├── RACEDAYERD.png                Entity Relationship Diagram (Section A)
│   ├── API_EndpointPlan.md           API endpoint plan (Section B)
│   └── RaceDay_DatabaseScript.sql    Schema + sample data script (Section C)
├── LICENSE
└── README.md
```

| Deliverable | File |
| --- | --- |
| Section A – ERD | [`docs/RACEDAYERD.png`](docs/RACEDAYERD.png) |
| Section B – API endpoint plan | [`docs/API_EndpointPlan.md`](docs/API_EndpointPlan.md) |
| Section C – Database script | [`docs/RaceDay_DatabaseScript.sql`](docs/RaceDay_DatabaseScript.sql) |

---

## 5. How to run the database script

1. Open `docs/RaceDay_DatabaseScript.sql` in SQL Server Management Studio (or Azure Data Studio).
2. Connect to a local SQL Server instance.
3. Execute the full script.

The script drops and recreates `RaceDayDb`, creates all six tables, and seeds sample organisers, participants, events, categories, enrolments and results.

---

## 6. Next steps

| Part | Focus |
| --- | --- |
| **Part 2** | ASP.NET Core Web API implementing the endpoints in `API_EndpointPlan.md` against `RaceDayDb` |
| **Part 3** | MVC front end, file uploads (profile picture and event banner), and Docker packaging |


---
## 7. Part 1 deliverables

### Section A - Entity Relationship Diagram

`docs/ERD.png`

The data model has **eight tables**, above the minimum of six. Every table shows its attributes, data types, primary key, foreign keys and constraints, and every relationship shows crow's foot cardinality.

| Table | Purpose |
| --- | --- |
| `Role` | Lookup table for the two roles: Organiser and Participant. |
| `Users` | All users of both roles, with a hashed password, contact details and date of birth. |
| `EventType` | Lookup table for Run, Walk and Cycle. |
| `Event` | An event created by one Organiser, with name, description, date, location, distance and type. |
| `EventCategory` | The age or distance categories defined for a specific event. |
| `EnrolmentStatus` | Lookup table for Pending, Confirmed and Cancelled, each with a colour code for the Part 3 badges. |
| `Enrolment` | The link recording which Participant entered which Event in which Category. |
| `Result` | The finish time and finishing position captured for a single enrolment. |

**Relationships and cardinality**

| Relationship | Cardinality | Reasoning |
| --- | --- | --- |
| `Role` → `Users` | One-to-many | Each user has exactly one role; each role is held by many users. |
| `Users` (Organiser) → `Event` | One-to-many | An Organiser creates many events; each event has one owning Organiser. |
| `EventType` → `Event` | One-to-many | Each event is one of Run, Walk or Cycle. |
| `Event` → `EventCategory` | One-to-many (cascade delete) | Categories belong to one event and are deleted with it. |
| `Event` → `Enrolment` | One-to-many | An event receives many enrolments. |
| `Users` (Participant) → `Enrolment` | One-to-many | A Participant enters many events over time. |
| `EventCategory` → `Enrolment` | One-to-many | Many Participants enter under the same category. |
| `EnrolmentStatus` → `Enrolment` | One-to-many | Each enrolment has exactly one status. |
| `Enrolment` → `Result` | One-to-one (optional, cascade delete) | An enrolment has at most one result, captured after the event. |

**Two design decisions worth noting**

1. **`Users` and `Event` form a many-to-many relationship that is resolved by `Enrolment`.** A Participant enters many events and an event has many Participants, so the two cannot be linked directly. `Enrolment` is the junction table, and because it also stores the selected `CategoryId`, the status and the race number, it holds the extra detail the brief asks for instead of being a plain link table.

2. **`Result` is linked to `Enrolment`, not to `Users`.** This makes it impossible to record a result for someone who never entered the event, and the event and category are already known through the enrolment. The `UNIQUE` constraint on `EnrolmentId` enforces the one-to-one.
---
### Section C — SQL database script

`docs/RaceDay-Database-Script.sql`

One script that creates and populates the database in SSMS. It drops `RaceDayDb` first, so it can be run again on a clean instance without errors.

- `CREATE TABLE` for all eight tables in the ERD, with every primary key, foreign key, `NOT NULL`, `UNIQUE`, `DEFAULT` and `CHECK` constraint written into the script.
- Rules enforced by the database instead of application code: a Participant cannot enter the same event twice (`UNIQUE (EventId, ParticipantId)`), a category name cannot repeat within an event, an event distance and entry fee cannot be negative, `MaxAge` cannot be lower than `MinAge`, and a finishing position cannot be greater than the total number of finishers.
- Sample data above the required minimum: **2 Organisers, 4 Participants, 3 Events** (Soweto Marathon, Cape Town Cycle Tour and Durban Beachfront Charity Walk — one of each event type), **10 categories** across those events, **8 enrolments** covering all three statuses, and **3 results** with finish times and positions.
- Three SELECT statements at the end that check the row count per table, list the enrolments per event and show the captured results.

**Running the script**

1. Open `docs/RaceDay-Database-Script.sql` in SQL Server Management Studio.
2. Connect to your SQL Server instance and execute the script (F5).
3. The SELECT statements at the end confirm the tables and sample data loaded correctly.

Or from the command line:

```bash
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "docs/RaceDay-Database-Script.sql"
```

### ERD and script alignment

The SQL script matches the ERD exactly — the same eight tables, the same attribute names and data types, and the same keys, constraints and cardinality. There are no deliberate differences to explain.

---
## 8. GitHub and CI/CD

The workflow at `.github/workflows/part1-validation.yml` runs on every push and pull request and checks that the repository is structured the way the brief requires. It fails the build if any of the following is untrue:

- the `docs/` folder exists;
- `docs/` contains an ERD image (`.png` or `.pdf`), an endpoint plan document and a `.sql` script;
- the SQL script contains `CREATE TABLE` statements for all eight tables, plus `PRIMARY KEY`, `FOREIGN KEY` and `INSERT` statements;
- the endpoint plan covers every required resource group (auth, profile, events, categories, enrolments, results);
- the README exists and describes both roles.

It then starts a SQL Server 2022 service container and runs the SQL script against it, so a green build proves the script works on a fresh instance and not only on my own machine.

**CI/CD green build screenshot**

![CI green build](docs/ci-green-build.png)

**Commit history:** a minimum of 20 meaningful commits for this part, each one a separate piece of work on the ERD, endpoint plan, SQL script, CI workflow or documentation.

---