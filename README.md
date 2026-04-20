# ClinicApp — Dental Clinic Management System

A full-featured dental clinic management web application built with ASP.NET Core 8 MVC and PostgreSQL.

---

## Features

- **Patient Management** — Patient profiles with medical history, allergies, chronic conditions, and notes
- **Appointments** — Schedule and track appointments with status tracking (Scheduled, Completed, Cancelled, No-Show)
- **Treatment Plans** — Create multi-step treatment plans per patient with lifecycle management
- **Treatments** — Track individual procedures (cleaning, filling, root canal, extraction, crown, implant, etc.) with before/after images
- **Doctor Schedules** — Manage doctor weekly availability
- **Inventory** — Track dental materials and supplies with low-stock alerts
- **Payments** — Record payments with multiple methods (Cash, Credit Card, Bank Transfer, Insurance)
- **Reports** — Export patient data to Excel and generate PDF schedules and treatment reports
- **Multi-Clinic** — Isolated data per clinic with per-clinic user management

## Roles

| Role      | Access                                              |
|-----------|-----------------------------------------------------|
| Manager   | Full access — manage staff, schedules, and reports  |
| Doctor    | Appointments, treatment plans, treatments           |
| Assistant | Support access to appointments and patient records  |

---

## Tech Stack

| Layer         | Technology                              |
|---------------|-----------------------------------------|
| Framework     | ASP.NET Core 8.0 MVC                   |
| Database      | PostgreSQL (via Npgsql EF Core)         |
| ORM           | Entity Framework Core 8.0              |
| Auth          | Cookie-based with role authorization   |
| Image Hosting | Cloudinary                              |
| PDF Export    | QuestPDF                                |
| Excel Export  | EPPlus                                  |
| Testing       | xUnit + Moq                             |
| Deployment    | Docker / Railway                        |

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL database (local or hosted)

### Local Setup

1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd ClinicApp
   ```

2. **Configure the database** in `ClinicApp.Web/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=clinicapp;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Set Cloudinary credentials** (for image uploads):
   ```json
   {
     "Cloudinary": {
       "CloudName": "...",
       "ApiKey": "...",
       "ApiSecret": "..."
     }
   }
   ```

4. **Run the application**
   ```bash
   dotnet restore
   dotnet run --project ClinicApp.Web/ClinicApp.Web.csproj
   ```

   The app will apply migrations and seed a default admin account on first run.

5. **Default credentials**
   - Email: `admin@clinic.com`
   - Password: `123456`

---

## Docker Deployment

```bash
docker build -t clinicapp .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..." \
  clinicapp
```

The app listens on port `8080` by default (configurable via the `PORT` environment variable).

---

## Project Structure

```
ClinicApp.Web/
├── Controllers/        # Auth, Patients, Appointments, Treatments, Doctors, Manager, Materials, Users
├── Models/             # Domain entities (Patient, Appointment, Treatment, Payment, etc.)
├── ViewModels/         # View-specific DTOs
├── Views/              # Razor views
├── Services/           # CloudinaryService, PrintService (PDF), ExportService (Excel)
├── Data/               # ApplicationDbContext and EF Core setup
├── Migrations/         # EF Core database migrations
└── wwwroot/            # Static assets (CSS, JS, images)
```

---

## Running Tests

```bash
dotnet test
```
