// calendar.js — Modern Clinic Calendar
let currentAppointmentId = null;

document.addEventListener('DOMContentLoaded', function () {
    initializeCalendar();
    setupCreateModal();
    setupPatientToggle();
    setupDetailsModal();
    setupDoctorAvailability();
    setupEditTimeCalc();
});

// ── Initialize FullCalendar ────────────────────────────────────────────
function initializeCalendar() {
    const calendarEl = document.getElementById('calendar');
    if (!calendarEl) return;

    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left:   'prev,next today',
            center: 'title',
            right:  'dayGridMonth,timeGridWeek,timeGridDay'
        },
        buttonText: {
            today: 'Today',
            month: 'Month',
            week:  'Week',
            day:   'Day'
        },
        height:       'auto',
        slotMinTime:  '07:00:00',
        slotMaxTime:  '22:00:00',
        slotDuration: '00:30:00',
        selectable:   true,
        nowIndicator: true,
        dayMaxEvents: 3,

        events: '/Appointments/GetCalendarEvents',

        eventContent: renderEventContent,

        eventClick: function (info) {
            showAppointmentDetails(info.event);
        },

        dateClick: function (info) {
            openCreateModalWithDate(info.dateStr, info.date);
        },

        select: function (info) {
            openCreateModalWithDateRange(info.start, info.end);
        },

        loading: function (isLoading) {
            const overlay = document.getElementById('calLoading');
            if (overlay) overlay.classList.toggle('active', isLoading);
        },

        eventsSet: function (events) {
            updateStats(events);
        },

        eventTimeFormat: {
            hour:   '2-digit',
            minute: '2-digit',
            hour12: false
        },
        slotLabelFormat: {
            hour:   '2-digit',
            minute: '2-digit',
            hour12: false
        }
    });

    calendar.render();
    window.calendarInstance = calendar;
}

// ── Custom Event Rendering ─────────────────────────────────────────────
function renderEventContent(arg) {
    const props    = arg.event.extendedProps;
    const isTimeGrid = arg.view.type.startsWith('timeGrid');

    const pill = document.createElement('div');
    pill.className = 'cal-event-pill';

    const dot = document.createElement('div');
    dot.className = 'cal-event-dot';

    const text = document.createElement('div');
    text.className = 'cal-event-text';
    text.textContent = props.patientName || arg.event.title;

    pill.appendChild(dot);
    pill.appendChild(text);

    if (isTimeGrid) {
        // Show time range and doctor in week/day view
        const timeEl = document.createElement('div');
        timeEl.className = 'cal-event-time';
        const start = arg.event.start;
        const end   = arg.event.end;
        if (start && end) {
            const fmt = t => t.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
            timeEl.textContent = `${fmt(start)} – ${fmt(end)}`;
        }
        pill.appendChild(timeEl);

        if (props.doctorName) {
            const doctorEl = document.createElement('div');
            doctorEl.className = 'cal-event-doctor';
            doctorEl.textContent = props.doctorName;
            pill.appendChild(doctorEl);
        }
    }

    return { domNodes: [pill] };
}

// ── Update Stats Bar ───────────────────────────────────────────────────
function updateStats(events) {
    const counts = { Scheduled: 0, Completed: 0, Cancelled: 0, NoShow: 0 };
    events.forEach(ev => {
        const s = ev.extendedProps?.status;
        if (s && Object.prototype.hasOwnProperty.call(counts, s)) counts[s]++;
    });
    const total = Object.values(counts).reduce((a, b) => a + b, 0);

    const set = (id, val) => {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    };

    set('stat-scheduled', counts.Scheduled);
    set('stat-completed',  counts.Completed);
    set('stat-cancelled',  counts.Cancelled);
    set('stat-noshow',     counts.NoShow);
    set('stat-total',      total);
}

// ── Appointment Details (View Mode) ───────────────────────────────────
function showAppointmentDetails(event) {
    currentAppointmentId = event.id;

    const props          = event.extendedProps;
    const viewMode       = document.getElementById('viewMode');
    const editMode       = document.getElementById('editMode');
    const modalTitle     = document.getElementById('modalTitle');
    const detailsActions = document.getElementById('detailsActions');

    const statusClass = {
        'Scheduled': 'status-scheduled',
        'Completed': 'status-completed',
        'Cancelled': 'status-cancelled',
        'NoShow':    'status-noshow'
    }[props.status] || 'status-scheduled';

    viewMode.style.display = 'block';
    editMode.style.display = 'none';
    modalTitle.textContent = 'Appointment Details';

    const fmt     = d => d?.toLocaleDateString('en-US',  { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' }) ?? '—';
    const fmtTime = d => d?.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false }) ?? '—';

    viewMode.innerHTML = `
        <div class="cal-detail-row">
            <div class="cal-detail-icon">👤</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Patient</div>
                <div class="cal-detail-value">${props.patientName ?? '—'}</div>
            </div>
        </div>
        <div class="cal-detail-row">
            <div class="cal-detail-icon">📞</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Phone</div>
                <div class="cal-detail-value">${props.patientPhone ?? '—'}</div>
            </div>
        </div>
        <div class="cal-detail-row">
            <div class="cal-detail-icon">🩺</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Doctor</div>
                <div class="cal-detail-value">${props.doctorName ?? '—'}</div>
            </div>
        </div>
        <div class="cal-detail-row">
            <div class="cal-detail-icon">🕐</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Date & Time</div>
                <div class="cal-detail-value">
                    ${fmt(event.start)}
                    <br><span style="color:var(--cal-text-muted);font-size:13px;">${fmtTime(event.start)} – ${fmtTime(event.end)}</span>
                </div>
            </div>
        </div>
        ${props.reason ? `
        <div class="cal-detail-row">
            <div class="cal-detail-icon">📋</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Reason</div>
                <div class="cal-detail-value">${props.reason}</div>
            </div>
        </div>` : ''}
        <div class="cal-detail-row">
            <div class="cal-detail-icon">🏷️</div>
            <div class="cal-detail-content">
                <div class="cal-detail-label">Status</div>
                <span class="cal-status-badge ${statusClass}">${props.status}</span>
            </div>
        </div>
    `;

    detailsActions.innerHTML = `
        <button type="button" class="cal-btn cal-btn-success" onclick="switchToEditMode()">Edit</button>
        <button type="button" class="cal-btn cal-btn-danger"  onclick="deleteAppointment(${event.id})">Delete</button>
    `;

    document.getElementById('detailsModal').classList.add('active');
}

// ── Edit Mode ──────────────────────────────────────────────────────────
function switchToEditMode() {
    fetch('/Appointments/GetAppointment/' + currentAppointmentId)
        .then(r => r.json())
        .then(data => {
            document.getElementById('viewMode').style.display  = 'none';
            document.getElementById('editMode').style.display  = 'block';
            document.getElementById('modalTitle').textContent  = 'Edit Appointment';

            document.getElementById('EditId').value             = data.id;
            document.getElementById('EditPatientName').value    = data.patientName;
            document.getElementById('EditDoctorId').value       = data.doctorId;
            document.getElementById('EditStartTime').value      = data.startTime;
            document.getElementById('EditEndTime').value        = data.endTime;
            document.getElementById('EditStatus').value         = data.status;
            document.getElementById('EditReasonForVisit').value = data.reasonForVisit ?? '';
            document.getElementById('EditNotes').value          = data.notes ?? '';

            document.getElementById('detailsActions').innerHTML = `
                <button type="button" class="cal-btn cal-btn-ghost" onclick="cancelEdit()">Cancel</button>
                <button type="submit" form="editForm" class="cal-btn cal-btn-primary">Save Changes</button>
            `;
        })
        .catch(() => showToast('Could not load appointment data.', 'error'));
}

function cancelEdit() {
    window.calendarInstance?.refetchEvents();
    closeDetailsModal();
}

// ── Delete ─────────────────────────────────────────────────────────────
function deleteAppointment(id) {
    if (!confirm('Delete this appointment? This action cannot be undone.')) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const form  = document.createElement('form');
    form.method = 'POST';
    form.action = '/Appointments/Delete/' + id;

    const tok  = document.createElement('input');
    tok.type   = 'hidden';
    tok.name   = '__RequestVerificationToken';
    tok.value  = token;
    form.appendChild(tok);
    document.body.appendChild(form);
    form.submit();
}

// ── Details Modal ──────────────────────────────────────────────────────
function setupDetailsModal() {
    document.getElementById('closeDetailsBtn')
        ?.addEventListener('click', closeDetailsModal);

    document.getElementById('detailsModal')
        ?.addEventListener('click', e => {
            if (e.target === e.currentTarget) closeDetailsModal();
        });
}

function closeDetailsModal() {
    document.getElementById('detailsModal').classList.remove('active');
}

// ── Create Modal ───────────────────────────────────────────────────────
function setupCreateModal() {
    const modal     = document.getElementById('appointmentModal');
    const openBtn   = document.getElementById('openModalBtn');
    const closeBtn  = document.getElementById('closeModalBtn');
    const cancelBtn = document.getElementById('cancelModalBtn');

    const open = () => {
        resetCreateForm();
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    };

    const close = () => {
        modal.style.display = 'none';
        document.body.style.overflow = '';
    };

    openBtn?.addEventListener('click', open);
    closeBtn?.addEventListener('click', close);
    cancelBtn?.addEventListener('click', close);
    modal?.addEventListener('click', e => { if (e.target === modal) close(); });
}

function openCreateModalWithDate(dateStr, dateObj) {
    resetCreateForm();
    const d = new Date(dateObj);
    d.setHours(9, 0, 0, 0);
    document.getElementById('StartTime').value = formatLocal(d);
    d.setMinutes(d.getMinutes() + 30);
    document.getElementById('EndTime').value = formatLocal(d);

    document.getElementById('appointmentModal').style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

function openCreateModalWithDateRange(startDate, endDate) {
    resetCreateForm();
    document.getElementById('StartTime').value = formatLocal(startDate);
    document.getElementById('EndTime').value   = formatLocal(endDate);

    document.getElementById('appointmentModal').style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

function resetCreateForm() {
    const existingRadio = document.getElementById('existingPatientRadio');
    if (existingRadio) {
        existingRadio.checked = true;
        existingRadio.dispatchEvent(new Event('change'));
    }

    ['ExistingPatientId', 'DoctorId', 'StartTime', 'EndTime'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });

    document.querySelectorAll('textarea[name^="NewAppointment"]')
        .forEach(t => (t.value = ''));
    document.querySelectorAll('input[name^="NewAppointment.NewPatient"]')
        .forEach(i => (i.value = ''));

    const note = document.getElementById('doctorAvailabilityNote');
    if (note) { note.textContent = ''; note.className = 'cal-doctor-note'; }
}

// ── Patient Toggle ─────────────────────────────────────────────────────
function setupPatientToggle() {
    const existingRadio   = document.getElementById('existingPatientRadio');
    const newRadio        = document.getElementById('newPatientRadio');
    const existingSection = document.getElementById('existing-patient-section');
    const newSection      = document.getElementById('new-patient-section');

    existingRadio?.addEventListener('change', () => {
        existingSection.style.display = 'block';
        newSection.style.display      = 'none';
    });

    newRadio?.addEventListener('change', () => {
        existingSection.style.display = 'none';
        newSection.style.display      = 'block';
    });
}

// ── Doctor Availability ────────────────────────────────────────────────
function setupDoctorAvailability() {
    const startInput = document.getElementById('StartTime');
    const endInput   = document.getElementById('EndTime');

    startInput?.addEventListener('change', function () {
        if (this.value) {
            const d = new Date(this.value);
            d.setMinutes(d.getMinutes() + 30);
            if (endInput) endInput.value = formatLocal(d);
        }
        loadAvailableDoctors();
    });

    endInput?.addEventListener('change', loadAvailableDoctors);
}

function loadAvailableDoctors() {
    const startInput   = document.getElementById('StartTime');
    const endInput     = document.getElementById('EndTime');
    const doctorSelect = document.getElementById('DoctorId');
    const doctorNote   = document.getElementById('doctorAvailabilityNote');

    const start = startInput?.value ?? '';
    const end   = endInput?.value   ?? '';
    if (!start || !end) return;

    doctorNote.textContent = 'Checking availability…';
    doctorNote.className   = 'cal-doctor-note loading';
    doctorSelect.innerHTML = '<option value="">Loading…</option>';

    const url = `${window.appEndpoints.getAvailableDoctors}?startTime=${encodeURIComponent(start)}&endTime=${encodeURIComponent(end)}`;

    fetch(url)
        .then(r => r.json())
        .then(doctors => {
            if (doctors.length === 0) {
                doctorSelect.innerHTML = '<option value="">No doctors available at this time</option>';
                doctorNote.textContent = 'No doctors available for this slot.';
                doctorNote.className   = 'cal-doctor-note error';
            } else {
                doctorSelect.innerHTML = '<option value="">— Select Doctor —</option>';
                doctors.forEach(d => {
                    const opt = document.createElement('option');
                    opt.value       = d.value;
                    opt.textContent = d.text;
                    doctorSelect.appendChild(opt);
                });
                doctorNote.textContent = `${doctors.length} doctor${doctors.length > 1 ? 's' : ''} available.`;
                doctorNote.className   = 'cal-doctor-note success';
            }
        })
        .catch(() => {
            doctorNote.textContent = 'Could not load doctors.';
            doctorNote.className   = 'cal-doctor-note error';
        });
}

// ── End Time Auto-Calc ─────────────────────────────────────────────────
function setupEditTimeCalc() {
    document.getElementById('EditStartTime')
        ?.addEventListener('change', () => calculateEndTime('EditStartTime', 'EditEndTime'));
}

function calculateEndTime(startId, endId) {
    const start = document.getElementById(startId);
    const end   = document.getElementById(endId);
    if (start?.value) {
        const d = new Date(start.value);
        d.setMinutes(d.getMinutes() + 30);
        end.value = formatLocal(d);
    }
}

// ── Helpers ────────────────────────────────────────────────────────────
function formatLocal(dt) {
    const y  = dt.getFullYear();
    const mo = String(dt.getMonth() + 1).padStart(2, '0');
    const d  = String(dt.getDate()).padStart(2, '0');
    const h  = String(dt.getHours()).padStart(2, '0');
    const mi = String(dt.getMinutes()).padStart(2, '0');
    return `${y}-${mo}-${d}T${h}:${mi}`;
}

// ── Toast Notifications ────────────────────────────────────────────────
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const icons = { success: '✓', error: '✕', warning: '!', info: 'ℹ' };

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
        <span class="toast-icon">${icons[type] ?? '•'}</span>
        <span class="toast-message">${message}</span>
        <button class="toast-dismiss" aria-label="Dismiss">✕</button>
    `;

    toast.querySelector('.toast-dismiss').addEventListener('click', () => dismissToast(toast));
    container.appendChild(toast);

    setTimeout(() => dismissToast(toast), 4500);
}

function dismissToast(toast) {
    toast.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
    toast.style.opacity    = '0';
    toast.style.transform  = 'translateX(20px)';
    setTimeout(() => toast.remove(), 260);
}

// ── PDF / Excel ────────────────────────────────────────────────────────
function printWeekPdf() {
    const calendar = window.calendarInstance;
    if (!calendar) { showToast('Calendar not ready.', 'error'); return; }
    const dateStr = calendar.getDate().toISOString().split('T')[0];
    window.open(`/Appointments/PrintWeekSchedulePdf?weekStart=${dateStr}`, '_blank');
}

function exportWeekExcel() {
    const calendar = window.calendarInstance;
    if (!calendar) { showToast('Calendar not ready.', 'error'); return; }
    const dateStr = calendar.getDate().toISOString().split('T')[0];
    window.location.href = `/Appointments/ExportWeekScheduleExcel?weekStart=${dateStr}`;
}
