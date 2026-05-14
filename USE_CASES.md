# DentalTrack — Use Case Scenarios

## Overview

DentalTrack is a multi-role dental clinic management system.  
Three roles exist: **Manager**, **Doctor**, and **Assistant**.  
All data is scoped to the clinic — users from different clinics never see each other's data.

---

## Actors

| Actor | Description |
|-------|-------------|
| **Manager** | Full access — manages users, materials, finances, and clinic settings |
| **Doctor** | Sees own appointments and patients; manages treatments and plans |
| **Assistant** | Schedules appointments and views patient info |

---

## UC-01 · Doctor Login and Calendar Overview

**Actor:** Doctor  
**Goal:** Log in and get an overview of today's schedule

### Steps
1. Doctor navigates to the clinic URL and is redirected to the **Login** page.
2. Doctor enters email `sarah@clinic.com` and password `123456` → clicks **Sign In**.
3. System authenticates and redirects to the **Doctor Dashboard**.
4. Doctor clicks **Appointments → Calendar** in the sidebar.
5. The weekly calendar loads showing all appointments assigned to this doctor, color-coded by status:
   - 🔵 Scheduled · 🟢 Completed · 🔴 Cancelled · 🟡 No Show
6. Doctor clicks **Today** to jump to the current week.
7. Doctor sees a time slot at 10:00 with patient **Ahmed Al-Rashid** for "Routine cleaning".
8. Doctor clicks the appointment block → a slide-in detail panel opens showing:
   - Patient name, phone number
   - Start / end time
   - Reason for visit
   - Status badge

**Result:** Doctor has a clear overview of the day's schedule within seconds of logging in.

---

## UC-02 · Book a New Appointment from the Calendar

**Actor:** Doctor or Assistant  
**Goal:** Schedule a new appointment for an existing patient

### Steps
1. From the Calendar view, doctor clicks **+ New Appointment** (top-right toolbar).
2. The **New Appointment** modal opens with a white dialog.
3. Doctor selects **Existing Patient** tab → picks **Maria Garcia** from the dropdown.
4. Doctor sets **Start Time** to tomorrow at 11:00.
   - End time auto-fills to 11:30.
   - System immediately checks Maria's schedule → shows **✓ Patient is free at this time**.
5. System loads available doctors for that slot → doctor selects **Dr. Michael Chen**.
6. Doctor types **Reason for Visit**: "Root canal follow-up".
7. Doctor clicks **Save Appointment**.
8. Modal closes, calendar refreshes, and the new appointment appears as a blue block.

### Alternative — Patient Already Booked
- At step 4, if Maria already has an appointment at 11:00, the modal shows:
  > ⚠ Patient already has an appointment Tue 15 Apr, 11:00–11:30
- The doctor must pick a different time before saving.

---

## UC-03 · Register a New Patient During Booking

**Actor:** Doctor or Assistant  
**Goal:** Create a new patient record and book their first appointment at the same time

### Steps
1. Open **+ New Appointment** modal.
2. Select the **New Patient** tab.
3. Fill in: First Name, Last Name, Phone, Gender, Date of Birth.
4. Set appointment time and select an available doctor.
5. Click **Save Appointment**.
6. System creates the patient record and the appointment in one action.
7. Patient appears in the Patients list from this point on.

---

## UC-04 · View Patient Profile and Medical History

**Actor:** Doctor  
**Goal:** Review a patient's full medical record before a procedure

### Steps
1. Doctor clicks **Patients** in the sidebar.
2. Finds **John Smith** using the search bar.
3. Clicks the patient row → opens the **Patient Profile** page.
4. Profile shows:
   - Personal info (DOB, gender, phone, address)
   - Medical notes, allergies (**Penicillin** flagged in red), chronic diseases (**Diabetes**)
   - Tooth chart with colour-coded tooth status (FDI numbering)
   - List of past and upcoming appointments
   - All treatments and their statuses
   - Payment history

**Result:** Doctor has complete clinical context before the appointment begins.

---

## UC-05 · Create a Treatment Plan

**Actor:** Doctor  
**Goal:** Plan a multi-step treatment for a patient and track progress

### Steps
1. From the Patient Profile of **Ahmed Al-Rashid**, doctor clicks **New Treatment Plan**.
2. Doctor fills in:
   - Title: "Comprehensive Care – May 2026"
   - Description: "Full restoration including cleaning, filling, and whitening"
   - Estimated Total Cost: 3,500
3. Plan is created with status **Active**.
4. Doctor adds individual treatments to the plan:
   - Step 1 — Full Mouth Cleaning · Type: Cleaning · Cost: 300 · Priority: Medium
   - Step 2 — Composite Filling #36 · Type: Filling · Cost: 450 · Priority: High
   - Step 3 — Teeth Whitening · Type: Whitening · Cost: 800 · Priority: Low
5. Each treatment shows as **Planned** in the plan's treatment list.
6. As sessions happen, doctor marks each treatment **In Progress** → **Completed**.
7. When all treatments are done, doctor marks the plan itself as **Completed**.

---

## UC-06 · Record a Treatment with Before/After Images

**Actor:** Doctor  
**Goal:** Document a completed procedure with photos

### Steps
1. Open a treatment from a patient's profile or treatment plan.
2. Click **Edit Treatment**.
3. Upload a **Before** image (tooth X-ray or photo via Cloudinary).
4. Perform the procedure.
5. Upload an **After** image.
6. Set status to **Completed**, fill actual cost.
7. Save — images are stored in Cloudinary and linked to the treatment record.

---

## UC-07 · Record a Payment

**Actor:** Doctor or Manager  
**Goal:** Record a payment for a completed treatment

### Steps
1. Navigate to **Payments** in the sidebar.
2. Click **New Payment**.
3. Select patient **Maria Garcia**.
4. Link to treatment: **Root Canal #36** (Invoice auto-assigned: #1006).
5. Enter amount: **1,200**.
6. Select payment method: **Bank Transfer**.
7. Set status: **Paid**.
8. Click **Save**.

### Result
- Payment appears in the payments list with a green "Paid" badge.
- Patient's payment history updates.

### Alternative — Partial / Pending Payment
- Set status to **Pending** and amount to a deposit (e.g., 600).
- Payment shows with an orange "Pending" badge.
- Manager can follow up and update to Paid later.

---

## UC-08 · Manager Adds a New Doctor

**Actor:** Manager  
**Goal:** Onboard a new doctor to the clinic

### Steps
1. Manager logs in with `admin@clinic.com` / `123456`.
2. Navigates to **Users** in the sidebar.
3. Clicks **Add User**.
4. Fills in: First Name, Last Name, Email, Phone, Role = **Doctor**, Password.
5. Saves — new doctor appears in the user list.
6. Manager navigates to **Doctor Schedule** and sets the new doctor's working days and hours.

**Result:** The doctor can now log in and will appear in the available-doctors list when appointments are booked during their scheduled hours.

---

## UC-09 · Two Doctors Working at the Same Time (Calendar Overlap)

**Actor:** Manager (viewing all appointments)  
**Goal:** Confirm that the calendar correctly shows simultaneous appointments

### Scenario
- Dr. Sarah has **Ahmed Al-Rashid** booked at 10:00–11:00.
- Dr. Michael has **Maria Garcia** booked at 10:00–11:00.

### What the Calendar Shows
- In the day/week column for that day, both appointments appear **side by side** in the same time slot — each takes half the column width.
- Clicking either block opens its detail panel independently.

---

## UC-10 · Manage Clinic Materials (Inventory)

**Actor:** Manager (full access) · Doctor (view + update quantity only)

### Manager Scenario
1. Manager clicks **Materials** in the sidebar.
2. Sees the stock table: Latex Gloves (500 pcs ✅), Anesthetic Cartridges (8 boxes ⚠ Low).
3. Clicks **Add Material** → adds "Composite Resin Shade A3" with minimum limit 5.
4. Clicks **Edit** on an existing material to update its name or unit.
5. Clicks **Remove** to delete an obsolete material.
6. Clicks **Export to Excel** to download a full inventory report.

### Doctor Scenario
1. Doctor clicks **Materials** in the sidebar (uses Doctor layout).
2. Sees the same stock table but **Add**, **Edit**, **Remove**, and **Export** buttons are hidden.
3. Clicks **+/-** next to Gloves → updates quantity by -10 with note "Used during procedures".
4. History row expands to show the change log.

---

## UC-11 · Export Weekly Schedule

**Actor:** Manager or Doctor  
**Goal:** Print or share the week's appointment schedule

### Steps
1. From the Calendar view, click **Print PDF** → opens a formatted PDF in a new tab showing all appointments for the current week, grouped by day and doctor.
2. Alternatively, click **Export** → downloads an Excel file with the same data.

---

## UC-12 · Access Denied Handling

**Actor:** Any user attempting to access a restricted page

### Scenario
- A Doctor tries to navigate directly to `/Users` (Manager-only).
- Instead of a full error page, a small **centered modal dialog** appears over a blurred backdrop:
  > 🔒 **Access Denied**  
  > You don't have permission to view this page.  
  > [ Go Back ]
- Clicking **Go Back** returns the user to the previous page.

---

## Summary Table

| Use Case | Manager | Doctor | Assistant |
|----------|:-------:|:------:|:---------:|
| Login | ✅ | ✅ | ✅ |
| View Calendar | ✅ (all) | ✅ (own) | ✅ (all) |
| Book Appointment | ✅ | ✅ | ✅ |
| View Patient Profile | ✅ | ✅ | ✅ |
| Create Treatment Plan | ✅ | ✅ | ❌ |
| Record Treatment | ✅ | ✅ | ❌ |
| Record Payment | ✅ | ✅ | ❌ |
| Add / Edit / Delete Material | ✅ | ❌ | ❌ |
| Update Material Quantity | ✅ | ✅ | ❌ |
| Manage Users | ✅ | ❌ | ❌ |
| Export Reports | ✅ | ✅ | ❌ |
