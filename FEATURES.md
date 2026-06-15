# ClinicApp — Full Feature Reference

## Roles & Access

| Role | Dashboard | Patients | Appointments | Payments | Materials | Attendance | Users |
|------|-----------|----------|--------------|----------|-----------|------------|-------|
| **Manager** | ✅ Full | ✅ + Delete + Profile | ✅ + Delete | ✅ Full + Delete | ✅ Full + Delete | ✅ View all + Delete | ✅ Full |
| **Doctor** | ✅ Own | ✅ No delete, No profile | ✅ Own only, No delete | ✅ Own only, No delete | ✅ View only | ✅ Own (MyAttendance) | ❌ |
| **Assistant** | ✅ Full | ✅ No delete, No profile | ✅ No delete | ✅ Add/Edit, No delete | ✅ View only | ✅ View all, No delete | ❌ |

---

## Auth

| Action | Description |
|--------|-------------|
| `GET  /Auth/Login` | Show login form |
| `POST /Auth/Login` | Authenticate; redirect → Manager/Doctor/Assistant dashboard based on role |
| `POST /Auth/Logout` | Sign out → Login page |
| `GET  /Auth/AccessDenied` | 403 error page |

Default generated password for new users: **123456**

---

## Dashboards

### Manager — `/Manager`
- Today's appointments count + list
- Total patients & doctors
- Recent 5 patients
- Low stock materials (qty ≤ minimum)
- Doctor schedules management link

### Doctor — `/Doctor`
- Today's own appointments
- Own patient count
- Pending appointments
- Recent 5 patients
- Low stock alert (read-only)

### Assistant — `/Assistant`
- Same stats as Manager dashboard
- No Users management link

---

## Appointments — `/Appointments`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | All | List all appointments; Doctor sees only their own |
| `GET  Calendar` | All | FullCalendar monthly/weekly view |
| `GET  GetCalendarEvents` | All | JSON feed for FullCalendar |
| `GET  GetAppointment/{id}` | All | Single appointment JSON |
| `GET  GetAvailableDoctors` | All | Check doctor availability for a time slot; respects DoctorSchedule |
| `GET  CheckPatientConflict` | All | Warn if patient has overlapping appointment |
| `GET  GetDoctorsListAsync` | All | Doctors dropdown JSON |
| `GET  GetPatientsListAsync` | All | Patients dropdown JSON |
| `POST Create` | All | Create appointment; supports inline patient creation; validates doctor & patient conflicts |
| `POST Edit` | All | Update appointment (time, status, notes, doctor) |
| `POST Delete` | **Manager only** | Delete appointment |
| `GET  PrintWeekSchedulePdf` | All | PDF of weekly schedule |
| `GET  ExportWeekScheduleExcel` | All | Excel of weekly schedule |

**Conflict rules:**
- Doctor cannot be double-booked at the same time slot
- Patient cannot have two appointments at the same time
- Doctor working hours checked against DoctorSchedule

---

## Patients — `/Patients`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | Manager, Doctor, Assistant | Patient list with search |
| `POST Add` | Manager, Doctor, Assistant | Create patient (name, ID, phone, DOB, gender, address, medical history, allergies, chronic diseases) |
| `POST Edit` | Manager, Doctor, Assistant | Update patient info |
| `POST Delete` | **Manager only** | Delete patient record |
| `GET  Profile/{id}` | **Manager, Doctor** | Full patient profile with teeth charting |
| `POST UpdateTooth` | Manager, Doctor, Assistant | Update single tooth status + notes |

**Tooth statuses:** Healthy · Caries · Filled · Crown · RootCanal · Missing · Extraction · Other

**Note:** Assistant sees patient list but cannot access individual patient profiles.

---

## Payments — `/Payments`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | Manager, Doctor, Assistant | Payment list with filters (patient, status, date range); stats panel + outstanding balances |
| `POST Create` | **Manager only** | Create payment (patient, doctor, amount, method, status, optional appointment/treatment link) |
| `POST Edit` | **Manager only** | Update payment details |
| `POST Delete` | **Manager only** | Delete payment |
| `GET  Treatment/{id}` | Manager, Doctor, Assistant | Treatment payment breakdown — total cost, paid, remaining, progress bar, installment list |
| `POST AddInstallment` | Manager, Assistant | Add payment installment; blocks overpayment |
| `POST EditInstallment` | Manager, Assistant | Edit installment amount/date/notes |
| `POST DeleteInstallment` | **Manager only** | Delete installment |
| `GET  PatientTreatments` | Manager, Doctor, Assistant | JSON — patient's treatments with payment progress % |
| `GET  GetPatientAppointments` | **Manager only** | JSON — patient's appointments for payment linking |

**Payment methods:** Cash · Credit Card · Bank Transfer · Insurance  
**Payment statuses:** Paid · Pending · Refunded

**Overpayment guard:** Server blocks AddInstallment if amount > remaining balance.  
**Fully-paid indicator:** When remaining = 0, "+ Add Payment" is replaced with "Treatment Fully Paid" badge.

---

## Materials / Inventory — `/Materials`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | Manager, Doctor, Assistant | Stock list with quantities; low-stock highlighted |
| `POST Add` | **Manager only** | Add new material with optional purchase invoice + image |
| `POST UpdateQuantity` | Manager, Doctor, Assistant | Adjust stock (increase/decrease); logs to history; optionally creates invoice |
| `GET  Invoices` | Manager, Doctor, Assistant | Purchase invoices filtered by material; totals (this month / year / all time) |
| `GET  InvoiceImage/{id}` | Manager, Doctor, Assistant | Signed URL redirect to invoice image in cloud storage |
| `GET  History/{id}` | Manager, Doctor, Assistant | JSON history of all quantity changes for a material |
| `POST Edit` | **Manager only** | Update material properties (name, unit, supplier, minimum limit) |
| `POST Delete` | **Manager only** | Delete material + all history + invoices |
| `GET  ExportExcel` | **Manager only** | Export full inventory + history to Excel |

**Invoice number format:** `MAT-YYYYMM-NNNN`

---

## Attendance — `/Attendance`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | Manager, Assistant | Monthly calendar view; filter by doctor + month/year; prev/next navigation |
| `GET  MyAttendance` | **Doctor only** | Own monthly attendance calendar with today banner |
| `POST Save` | Manager, Doctor, Assistant | Upsert daily record (Present/Absent/DayOff + check-in/check-out times); Doctor can only save own records |
| `POST Delete` | **Manager only** | Delete a daily record |

**Summary cards:** Days Present · Days Absent · Days Off · Total Hours Worked · Treatments Completed (avg/day)

**Working hours** are auto-calculated from CheckIn/CheckOut (computed property, not stored).  
**Unique constraint:** One record per doctor per date.

---

## Doctor Schedules — `/Manager/DoctorSchedules`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  DoctorSchedules` | **Manager only** | Weekly schedule grid for all doctors (Mon–Sun) |
| `POST SaveDoctorSchedules` | **Manager only** | Upsert working hours per doctor per day |

Used by appointment booking to filter available doctors by time slot.

---

## User Management — `/Users`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | **Manager only** | List all staff (doctors + assistants) |
| `POST Add` | **Manager only** | Create user (name, email, phone, role); default password: 123456 |
| `POST Edit` | **Manager only** | Update user info |
| `POST Delete` | **Manager only** | Remove user account |

**Roles available:** Manager · Doctor · Assistant

---

## Treatment Plans — `/TreatmentPlan`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index/{patientId}` | Manager, Doctor | List all plans for a patient |
| `POST CreatePlan` | Manager, Doctor | New plan (title, description, start date); default status: Draft |
| `GET  Details/{planId}` | Manager, Doctor | Plan detail with treatment list, progress %, cost breakdown |
| `POST AddTreatment` | Manager, Doctor | Add treatment step (type, tooth, title, cost, priority) |
| `POST UpdateTreatment` | Manager, Doctor | Update status, actual cost, notes; upload before/after images to Cloudinary |
| `POST DeleteTreatment` | Manager, Doctor | Remove treatment step; updates plan total cost |
| `POST UpdatePlanStatus` | Manager, Doctor | Change plan status (Draft → Active → Completed / Cancelled) |
| `POST DeletePlan` | **Doctor only** | Delete entire plan |

**Treatment types:** Cleaning · Filling · Root Canal · Extraction · Crown · Bridge · Implant · Whitening · Orthodontics · Denture · Scaling · Other  
**Plan statuses:** Draft · Active · Completed · Cancelled

---

## Patient Files — `/patients/{patientId}/files`

| Action | Roles | Description |
|--------|-------|-------------|
| `GET  Index` | **Doctor only** | All files grouped by category with upload form |
| `POST Upload` | **Doctor only** | Upload file to cloud storage (rate-limited) |
| `GET  SignedUrl` | **Doctor only** | 20-minute signed download URL |
| `POST Delete` | **Doctor only** | Soft-delete file (marks DeletedAtUtc) + removes from cloud |
| `GET  Viewer` | **Doctor only** | 3D model viewer (.glb / .obj / .ply / .usdz) |
| `GET  XrayViewer` | **Doctor only** | X-ray image viewer |
| `GET  PanoViewer` | **Doctor only** | Panoramic X-ray viewer |
| `GET  Stream` | **Doctor only** | Backend proxy stream for 3D viewer |

**File categories:** Intraoral Photo · Panoramic X-ray · Bitewing X-ray · Periapical X-ray · CBCT Scan · Intraoral Scan · Medical Report · Prescription · Invoice · Other

---

## Data Models

### AppUser
`Id · FirstName · LastName · Email · Phone · PasswordHash · Role · ClinicId`

### Patient
`Id · IdNumber · FirstName · LastName · Phone · Email · DateOfBirth · Gender · Address · MedicalNotes · Allergies · ChronicDiseases · ClinicId`

### Appointment
`Id · StartTime · EndTime · Status · ReasonForVisit · Notes · DoctorNotes · PatientId · DoctorId · ClinicId · CreatedAt · UpdatedAt`

### Payment
`Id · InvoiceNumber · Amount · Method · Status · PaymentDate · Notes · PatientId · DoctorId · TreatmentId? · AppointmentId? · ClinicId`

### Treatment
`Id · Title · Description · Cost · EstimatedCost · TreatmentDate · CompletedAt · ToothNumber · Type · Status · Priority · BeforeImageUrl · AfterImageUrl · PatientId · DoctorId · ClinicId · TreatmentPlanId?`

### TreatmentPlan
`Id · Title · Description · TotalEstimatedCost · Status · CreatedAt · StartDate · CompletedAt · PatientId · DoctorId · ClinicId`

### Material
`Id · Name · Description · Quantity · MinimumLimit · Unit · Supplier · ClinicId`

### MaterialHistory
`Id · MaterialId · QuantityChange · NewQuantity · Supplier · Note · CreatedAt · MaterialInvoiceId?`

### MaterialInvoice
`Id · InvoiceNumber · MaterialId · ClinicId · Supplier · Quantity · UnitPrice · TotalAmount · PaymentMethod · InvoiceDate · Notes · InvoiceImageObjectPath · CreatedAt`

### DoctorAttendance
`Id · DoctorId · ClinicId · Date · Status · CheckIn · CheckOut · Notes · CreatedAt · UpdatedAt`  
*Computed:* `WorkingHours = CheckOut - CheckIn`  
*Unique index:* `(DoctorId, Date)`

### DoctorSchedule
`Id · DoctorId · ClinicId · DayOfWeek · StartTime · EndTime · IsWorkingDay`

### PatientTooth
`Id · PatientId · ToothNumber · Status · Notes · UpdatedAt`

### PatientFile
`Id · PatientId · Category · OriginalFileName · StoredFileName · ObjectPath · ContentType · Extension · Size · UploadedAtUtc · DeletedAtUtc · Notes`

### Clinic
`Id · Name · Address`

---

## Enum Reference

| Enum | Values |
|------|--------|
| `UserRole` | Manager=1, Doctor=2, Assistant=3 |
| `AppointmentStatus` | Scheduled=1, Completed=2, Cancelled=3, NoShow=4 |
| `AttendanceStatus` | Present=1, Absent=2, DayOff=3 |
| `PaymentMethod` | Cash=1, CreditCard=2, BankTransfer=3, Insurance=4 |
| `PaymentStatus` | Paid=1, Pending=2, Refunded=3 |
| `TreatmentStatus` | Planned=1, InProgress=2, Completed=3, Cancelled=4 |
| `TreatmentType` | Cleaning=1, Filling=2, RootCanal=3, Extraction=4, Crown=5, Bridge=6, Implant=7, Whitening=8, Orthodontics=9, Denture=10, Scaling=11, Other=12 |
| `ToothStatus` | Healthy=1, Caries=2, Filled=3, Crown=4, RootCanal=5, Missing=6, Extraction=7, Other=8 |
| `PlanStatus` | Draft=1, Active=2, Completed=3, Cancelled=4 |
| `PatientFileCategory` | IntraoralPhoto=0, PanoramicXray=1, BitewingXray=2, PeriapicalXray=3, CbctScan=4, IntraoralScan=5, MedicalReport=6, Prescription=7, Invoice=8, Other=9 |

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core 8 MVC |
| Database | PostgreSQL via EF Core 8 (Npgsql) |
| Auth | Cookie-based, claim-driven (ClinicId, Role, FirstName, LastName) |
| Image storage | Cloudinary (treatment before/after images) |
| File storage | Cloud object storage with signed URLs (patient files, invoices) |
| Calendar UI | FullCalendar 6.1 |
| PDF export | Server-side PDF generation |
| Excel export | EPPlus |
| Tests | xUnit + Moq + EF Core InMemory — 221 tests |
| Layouts | 3 separate layouts: _ManagerLayout, _DoctorLayout, _AssistantLayout |

---

## Test Coverage — 221 Tests

| Suite | Count |
|-------|-------|
| AppointmentsController | ~90 |
| CalendarController | ~18 |
| PaymentsController | ~50 |
| MaterialsController | ~30 |
| AttendanceController | ~18 |
| PatientFilesController | ~15 |
