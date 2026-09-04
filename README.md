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
