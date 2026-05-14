// calendar.js — Custom clinic calendar (no external library)

// ── Constants ─────────────────────────────────────────────────
const PX_PER_MIN = 1.5;
const HOUR_START = 7;
const HOUR_END   = 21;
const DOC_PALETTE = ['#10b981', '#3b82f6', '#8b5cf6', '#f59e0b', '#ec4899', '#06b6d4'];

// ── State ─────────────────────────────────────────────────────
const CAL = {
    view:          'week',
    weekStart:     getWeekStart(new Date()),
    focusDate:     new Date(),
    filterDoc:     'all',
    showCancelled: false,
    openApptId:    null,
    events:        [],
    doctorColors:  {},
    colorIdx:      0,
};

// ── Bootstrap ─────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    fetchAllEvents().then(() => {
        renderAll();
        scrollToNow();
    });

    populateTimeSelect();
    applyDateMinima();
    setupCreateModal();
    setupEditModal();
    setupPatientToggle();
    setupDoctorAvailability();
    setupEditTimeCalc();
    startNowIndicatorRefresh();

    document.getElementById('prevBtn')?.addEventListener('click', () => {
        if (CAL.view === 'week') {
            CAL.weekStart.setDate(CAL.weekStart.getDate() - 7);
        } else {
            CAL.focusDate.setDate(CAL.focusDate.getDate() - 1);
        }
        renderAll();
    });

    document.getElementById('nextBtn')?.addEventListener('click', () => {
        if (CAL.view === 'week') {
            CAL.weekStart.setDate(CAL.weekStart.getDate() + 7);
        } else {
            CAL.focusDate.setDate(CAL.focusDate.getDate() + 1);
        }
        renderAll();
    });

    document.getElementById('todayBtn')?.addEventListener('click', () => {
        CAL.weekStart = getWeekStart(new Date());
        CAL.focusDate = new Date();
        renderAll();
        scrollToNow();
    });

    document.getElementById('weekBtn')?.addEventListener('click', () => { CAL.view = 'week'; renderAll(); });
    document.getElementById('dayBtn')?.addEventListener('click',  () => { CAL.view = 'day';  renderAll(); });

    document.getElementById('cancelledToggleBtn')?.addEventListener('click', () => {
        CAL.showCancelled = !CAL.showCancelled;
        const btn = document.getElementById('cancelledToggleBtn');
        if (btn) {
            btn.textContent = CAL.showCancelled ? 'Hide Cancelled' : 'Show Cancelled';
            btn.classList.toggle('active-filter', CAL.showCancelled);
        }
        renderAll();
    });

    document.getElementById('calGrid')
        ?.addEventListener('click', e => {
            if (!e.target.closest('.cal-event') && !e.target.closest('.cal-detail-panel')) {
                closeDetailPanel();
            }
        });
});

// ── Fetch events ──────────────────────────────────────────────
async function fetchAllEvents() {
    try {
        const res = await fetch('/Appointments/GetCalendarEvents');
        const raw = await res.json();
        CAL.events = raw.map(e => ({
            ...e,
            startDate: new Date(e.start),
            endDate:   new Date(e.end),
        }));
        CAL.events.forEach(e => {
            const doc = e.extendedProps?.doctorName;
            if (doc && !CAL.doctorColors[doc]) {
                CAL.doctorColors[doc] = DOC_PALETTE[CAL.colorIdx % DOC_PALETTE.length];
                CAL.colorIdx++;
            }
        });
    } catch {
        showToast('Could not load appointments.', 'error');
    }
}

async function refetchEvents() {
    await fetchAllEvents();
    renderAll();
}

// ── Render all ────────────────────────────────────────────────
function renderAll() {
    renderToolbar();
    renderDayHeader();
    renderGrid();
}

// ── Visible days ──────────────────────────────────────────────
function getVisibleDays() {
    if (CAL.view === 'day') {
        return [new Date(CAL.focusDate)];
    }
    return Array.from({ length: 7 }, (_, i) => {
        const d = new Date(CAL.weekStart);
        d.setDate(d.getDate() + i);
        return d;
    });
}

// ── Toolbar ───────────────────────────────────────────────────
function renderToolbar() {
    const days  = getVisibleDays();
    const first = days[0];
    const last  = days[days.length - 1];
    const label = document.getElementById('periodLabel');

    if (CAL.view === 'week') {
        const sameMonth = first.getMonth() === last.getMonth();
        label.textContent = sameMonth
            ? first.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
            : `${first.toLocaleDateString('en-US', { month: 'short' })} – ${last.toLocaleDateString('en-US', { month: 'short', year: 'numeric' })}`;
    } else {
        label.textContent = first.toLocaleDateString('en-US', {
            weekday: 'long', month: 'long', day: 'numeric', year: 'numeric'
        });
    }

    renderDoctorPills();

    document.getElementById('weekBtn').classList.toggle('active', CAL.view === 'week');
    document.getElementById('dayBtn').classList.toggle('active', CAL.view === 'day');
}

function renderDoctorPills() {
    const container = document.getElementById('doctorPills');
    if (!container) return;

    const docs = Object.keys(CAL.doctorColors);

    let html = `<button class="cal-pill ${CAL.filterDoc === 'all' ? 'active-all' : ''}" data-doc="all">All Doctors</button>`;

    docs.forEach(doc => {
        const color    = CAL.doctorColors[doc];
        const isActive = CAL.filterDoc === doc;
        const style    = isActive ? `border-color:${color};background:${color}18;color:${color};` : '';
        html += `<button class="cal-pill" data-doc="${escHtml(doc)}" style="${style}">
            ${isActive ? `<span class="cal-pill-dot" style="background:${color};"></span>` : ''}
            ${escHtml(doc)}
        </button>`;
    });

    container.innerHTML = html;

    container.querySelectorAll('.cal-pill').forEach(btn => {
        btn.addEventListener('click', () => {
            CAL.filterDoc = btn.dataset.doc;
            renderAll();
        });
    });
}

// ── Day header ────────────────────────────────────────────────
function renderDayHeader() {
    const header = document.getElementById('dayHeader');
    const days   = getVisibleDays();
    const today  = todayMidnight();

    let html = `<div class="cal-gutter-space"></div>`;

    days.forEach((day, i) => {
        const isToday  = day.getTime() === today.getTime();
        const isPast   = day < today && !isToday;
        const dayName  = day.toLocaleDateString('en-US', { weekday: 'short' }).toUpperCase();
        const dayNum   = day.getDate();
        const month    = day.toLocaleDateString('en-US', { month: 'short' });

        const dayStart = new Date(day); dayStart.setHours(0,0,0,0);
        const dayEnd   = new Date(day); dayEnd.setHours(23,59,59,999);
        const dayCount = CAL.events.filter(e => {
            if (!CAL.showCancelled && e.extendedProps?.status === 'Cancelled') return false;
            return e.startDate >= dayStart && e.startDate <= dayEnd;
        }).length;

        html += `
            <div class="cal-day-cell ${isToday ? 'today' : ''} ${isPast ? 'past' : ''}" data-idx="${i}">
                <span class="cal-day-name">${dayName}</span>
                <div class="cal-day-num-wrap">
                    <span class="cal-day-num ${isToday ? 'today' : ''}">${dayNum}</span>
                </div>
                <div class="cal-day-meta">
                    <span class="cal-month-label">${month}</span>
                    ${dayCount > 0 ? `<span class="cal-day-count">${dayCount}</span>` : ''}
                </div>
            </div>
        `;
    });

    header.innerHTML = html;

    header.querySelectorAll('.cal-day-cell').forEach((cell, i) => {
        cell.addEventListener('click', () => {
            CAL.focusDate = new Date(days[i]);
            CAL.view = 'day';
            renderAll();
        });
    });
}

// ── Grid ──────────────────────────────────────────────────────
function renderGrid() {
    const grid  = document.getElementById('calGrid');
    if (!grid) return;

    const days    = getVisibleDays();
    const totalH  = (HOUR_END - HOUR_START) * 60 * PX_PER_MIN;
    const today   = todayMidnight();

    const filtered = CAL.events.filter(e => {
        if (CAL.filterDoc !== 'all' && e.extendedProps?.doctorName !== CAL.filterDoc) return false;
        if (!CAL.showCancelled && e.extendedProps?.status === 'Cancelled') return false;
        return true;
    });

    // ── Time gutter ──
    let gutterHtml = `<div class="cal-gutter" style="height:${totalH}px">`;
    for (let h = HOUR_START; h < HOUR_END; h++) {
        const top = (h - HOUR_START) * 60 * PX_PER_MIN;
        gutterHtml += `<div class="cal-hour-label" style="top:${top}px">${pad(h)}:00</div>`;
    }
    gutterHtml += `</div>`;

    // ── Day columns ──
    let colsHtml = '';

    days.forEach((day, colIdx) => {
        const isToday  = day.getTime() === today.getTime();
        const isPast   = day < today && !isToday;
        const dayStart = new Date(day); dayStart.setHours(0,0,0,0);
        const dayEnd   = new Date(day); dayEnd.setHours(23,59,59,999);

        const dayEvents = filtered.filter(e =>
            e.startDate >= dayStart && e.startDate <= dayEnd
        );

        // Grid lines
        let lines = '';
        for (let h = HOUR_START; h < HOUR_END; h++) {
            const top  = (h - HOUR_START) * 60 * PX_PER_MIN;
            const half = top + 30 * PX_PER_MIN;
            lines += `<div class="cal-hour-line" style="top:${top}px"></div>
                      <div class="cal-half-line" style="top:${half}px"></div>`;
        }

        // Past-time shading: whole column for past days, partial for today
        let pastHtml = '';
        if (isPast) {
            pastHtml = `<div class="cal-past-overlay" style="height:${totalH}px"></div>`;
        } else if (isToday) {
            const pt = getNowTop();
            if (pt !== null && pt > 0)
                pastHtml = `<div class="cal-past-overlay" style="height:${pt}px"></div>`;
        }

        // Now indicator (today only)
        const nowTop = isToday ? getNowTop() : null;
        const nowHtml = nowTop !== null
            ? `<div class="cal-now-line" id="nowLine" style="top:${nowTop}px"><div class="cal-now-dot"></div></div>`
            : '';

        // Appointment blocks with overlap-aware column layout
        const layout   = computeOverlapLayout(dayEvents);
        let eventsHtml = dayEvents.map(ev => buildEventBlock(ev, layout)).join('');

        colsHtml += `<div class="cal-day-col ${isToday ? 'today' : ''} ${isPast ? 'past-day' : ''}"
                          style="height:${totalH}px"
                          data-col-idx="${colIdx}"
                          data-day="${day.toISOString().split('T')[0]}">
            ${lines}${pastHtml}${nowHtml}${eventsHtml}
        </div>`;
    });

    grid.innerHTML = gutterHtml + colsHtml;

    // Attach event listeners
    attachGridListeners(days);
}

function buildEventBlock(ev, layout) {
    const top    = getEventTop(ev.startDate);
    const height = getEventHeight(ev.startDate, ev.endDate);
    const doc    = ev.extendedProps?.doctorName ?? '';
    const color  = CAL.doctorColors[doc] || '#7A6860';
    const status = ev.extendedProps?.status || 'Scheduled';

    const isCancelled = status === 'Cancelled';
    const isDone      = status === 'Completed' || isCancelled;
    const bg          = isCancelled ? '#fef2f2' : isDone ? '#f1f5f9' : `${color}18`;
    const border      = isCancelled ? '#fca5a5' : isDone ? '#cbd5e1' : color;
    const text        = isCancelled ? '#ef4444' : isDone ? '#94a3b8' : color;
    const opacity     = isCancelled ? 'opacity:0.7;' : '';

    const name   = escHtml(ev.extendedProps?.patientName || ev.title || '');
    const reason = escHtml(ev.extendedProps?.reason || '');
    const showReason = height > 38 && reason;

    // Overlap-aware positioning
    const lane  = layout?.eventLane.get(ev.id)  ?? 0;
    const total = layout?.eventTotal.get(ev.id) ?? 1;
    const colW  = 100 / total;
    const leftPct  = lane * colW;
    const rightPct = 100 - (lane + 1) * colW;
    const leftStyle  = `calc(${leftPct}% + ${lane === 0 ? 4 : 2}px)`;
    const rightStyle = `calc(${rightPct}% + ${lane === total - 1 ? 4 : 2}px)`;

    const cancelledBadge = isCancelled ? `<span class="cal-event-cancelled-tag">Cancelled</span>` : '';

    return `<div class="cal-event ${isCancelled ? 'cal-event--cancelled' : ''}"
                 data-appt-id="${ev.id}"
                 style="top:${top}px;height:${height}px;left:${leftStyle};right:${rightStyle};background:${bg};border-left-color:${border};${opacity}"
                 tabindex="0"
                 aria-label="${name}">
        <div class="cal-event-name" style="color:${text}">${name}</div>
        ${showReason ? `<div class="cal-event-reason">${reason}</div>` : ''}
        ${cancelledBadge}
    </div>`;
}

function attachGridListeners(days) {
    const grid = document.getElementById('calGrid');

    // Click on appointment block → open panel
    grid.querySelectorAll('.cal-event').forEach(el => {
        el.addEventListener('click', e => {
            e.stopPropagation();
            openDetailPanel(parseInt(el.dataset.apptId));
        });
        el.addEventListener('keydown', e => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                openDetailPanel(parseInt(el.dataset.apptId));
            }
        });
    });

    // Click on empty column area → open create modal at that time
    grid.querySelectorAll('.cal-day-col').forEach((col, i) => {
        col.addEventListener('click', e => {
            if (e.target.closest('.cal-event')) return;
            const scroller  = document.getElementById('calGridScroller');
            const rect      = col.getBoundingClientRect();
            const relY      = e.clientY - rect.top + scroller.scrollTop;
            const totalMins = Math.round(relY / PX_PER_MIN / 15) * 15;
            const h = HOUR_START + Math.floor(totalMins / 60);
            const m = totalMins % 60;

            const clickedDay = new Date(days[i]);
            clickedDay.setHours(Math.min(h, HOUR_END - 1), m, 0, 0);

            if (clickedDay < new Date()) {
                showToast('Cannot schedule appointments in the past.', 'error');
                return;
            }
            openCreateModalWithDate(null, clickedDay);
        });
    });
}

// ── Overlap layout ────────────────────────────────────────────
// Returns { eventLane: Map<id,col>, eventTotal: Map<id,totalCols> }
function computeOverlapLayout(events) {
    const eventLane  = new Map();
    const eventTotal = new Map();

    if (!events.length) return { eventLane, eventTotal };

    // Sort by start time, then by longer events first
    const sorted = [...events].sort((a, b) =>
        a.startDate - b.startDate || b.endDate - a.endDate
    );

    // Greedy column assignment: place each event in the first free column
    const colEnd = []; // colEnd[i] = last endDate occupying column i

    for (const ev of sorted) {
        let col = 0;
        while (col < colEnd.length && colEnd[col] > ev.startDate) col++;
        eventLane.set(ev.id, col);
        colEnd[col] = ev.endDate;
    }

    // For each event, find the max column index among all overlapping events
    for (const ev of sorted) {
        let maxCol = eventLane.get(ev.id);
        for (const other of sorted) {
            if (other.id !== ev.id && eventsOverlap(ev, other)) {
                maxCol = Math.max(maxCol, eventLane.get(other.id));
            }
        }
        eventTotal.set(ev.id, maxCol + 1);
    }

    return { eventLane, eventTotal };
}

function eventsOverlap(a, b) {
    return a.startDate < b.endDate && a.endDate > b.startDate;
}

// ── Positioning helpers ───────────────────────────────────────
function getEventTop(start) {
    return ((start.getHours() - HOUR_START) * 60 + start.getMinutes()) * PX_PER_MIN;
}

function getEventHeight(start, end) {
    return Math.max(((end - start) / 60000) * PX_PER_MIN - 3, 24);
}

function getNowTop() {
    const now = new Date();
    const h = now.getHours();
    const m = now.getMinutes();
    if (h < HOUR_START || h >= HOUR_END) return null;
    return (h - HOUR_START) * 60 * PX_PER_MIN + m * PX_PER_MIN;
}

function startNowIndicatorRefresh() {
    setInterval(() => {
        const line = document.getElementById('nowLine');
        if (line) {
            const top = getNowTop();
            if (top !== null) line.style.top = `${top}px`;
        }
    }, 60000);
}

function scrollToNow() {
    const top = getNowTop();
    if (top === null) return;
    const scroller = document.getElementById('calGridScroller');
    if (scroller) scroller.scrollTop = Math.max(0, top - 120);
}

// ── Detail slide-in panel ─────────────────────────────────────
function openDetailPanel(apptId) {
    const ev = CAL.events.find(e => e.id == apptId);
    if (!ev) return;

    CAL.openApptId = apptId;

    const props   = ev.extendedProps ?? {};
    const doc     = props.doctorName ?? '';
    const color   = CAL.doctorColors[doc] || '#7A6860';
    const status  = props.status || 'Scheduled';

    const statusClass = {
        'Scheduled': 'status-scheduled',
        'Completed': 'status-completed',
        'Cancelled': 'status-cancelled',
        'NoShow':    'status-noshow',
    }[status] ?? 'status-scheduled';

    const fmt     = d => d.toLocaleDateString('en-US',  { weekday: 'short', month: 'short', day: 'numeric' });
    const fmtTime = d => d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });

    // Header (tinted with doctor color)
    document.getElementById('detailHeader').style.background  = `${color}18`;
    document.getElementById('detailHeader').style.borderBottomColor = `${color}33`;
    document.getElementById('detailHeader').innerHTML = `
        <button class="cal-detail-close" id="panelCloseBtn" aria-label="Close">&times;</button>
        <div class="cal-detail-patient">${escHtml(props.patientName ?? '—')}</div>
        ${props.reason ? `<div class="cal-detail-reason">${escHtml(props.reason)}</div>` : ''}
        <span class="cal-detail-status ${statusClass}">${status}</span>
    `;

    // Body rows
    document.getElementById('detailRows').innerHTML = `
        <div class="cal-detail-row">
            <span class="cal-detail-icon">🕐</span>
            <div class="cal-detail-info">
                <div class="cal-detail-info-label">Date & Time</div>
                <div class="cal-detail-info-value">${fmt(ev.startDate)}<br>${fmtTime(ev.startDate)} – ${fmtTime(ev.endDate)}</div>
            </div>
        </div>
        <div class="cal-detail-row">
            <span class="cal-detail-icon">👨‍⚕️</span>
            <div class="cal-detail-info">
                <div class="cal-detail-info-label">Doctor</div>
                <div class="cal-detail-info-value">${escHtml(doc || '—')}</div>
            </div>
        </div>
        <div class="cal-detail-row">
            <span class="cal-detail-icon">📞</span>
            <div class="cal-detail-info">
                <div class="cal-detail-info-label">Phone</div>
                <div class="cal-detail-info-value" style="direction:ltr;text-align:left">${escHtml(props.patientPhone || '—')}</div>
            </div>
        </div>
    `;

    // Footer
    document.getElementById('detailFooter').innerHTML = `
        <button class="cal-detail-btn-primary" id="panelEditBtn">Edit Appointment</button>
        <button class="cal-detail-btn-danger" id="panelDeleteBtn">Cancel Appointment</button>
    `;

    document.getElementById('panelCloseBtn').addEventListener('click', closeDetailPanel);
    document.getElementById('panelEditBtn').addEventListener('click', () => {
        closeDetailPanel();
        openEditModal(apptId);
    });
    document.getElementById('panelDeleteBtn').addEventListener('click', () => {
        deleteAppointment(apptId);
    });

    document.getElementById('detailPanel').classList.add('open');
}

function closeDetailPanel() {
    document.getElementById('detailPanel').classList.remove('open');
    CAL.openApptId = null;
}

// ── Edit modal ────────────────────────────────────────────────
function openEditModal(apptId) {
    fetch('/Appointments/GetAppointment/' + apptId)
        .then(r => r.json())
        .then(data => {
            document.getElementById('EditId').value             = data.id;
            document.getElementById('EditPatientName').value    = data.patientName;
            document.getElementById('EditDoctorId').value       = data.doctorId;
            document.getElementById('EditStartTime').value      = data.startTime;
            document.getElementById('EditEndTime').value        = data.endTime;
            document.getElementById('EditStatus').value         = data.status;
            document.getElementById('EditReasonForVisit').value = data.reasonForVisit ?? '';
            document.getElementById('EditNotes').value          = data.notes ?? '';

            document.getElementById('editModal').classList.add('active');
        })
        .catch(() => showToast('Could not load appointment data.', 'error'));
}

function setupEditModal() {
    document.getElementById('closeEditBtn')
        ?.addEventListener('click', () => document.getElementById('editModal').classList.remove('active'));

    document.getElementById('editModal')
        ?.addEventListener('click', e => {
            if (e.target === e.currentTarget)
                e.currentTarget.classList.remove('active');
        });
}

// ── Delete ────────────────────────────────────────────────────
function deleteAppointment(id) {
    if (!confirm('Delete this appointment? This cannot be undone.')) return;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const form  = document.createElement('form');
    form.method = 'POST';
    form.action = '/Appointments/Delete/' + id;
    const tok   = Object.assign(document.createElement('input'), {
        type: 'hidden', name: '__RequestVerificationToken', value: token
    });
    form.appendChild(tok);
    document.body.appendChild(form);
    form.submit();
}

// ── Create modal ──────────────────────────────────────────────
function setupCreateModal() {
    const modal     = document.getElementById('appointmentModal');
    const openBtn   = document.getElementById('openModalBtn');
    const closeBtn  = document.getElementById('closeModalBtn');
    const cancelBtn = document.getElementById('cancelModalBtn');

    const open  = () => { resetCreateForm(); applyDateMinima(); modal.classList.add('active'); document.body.style.overflow = 'hidden'; };
    const close = () => { modal.classList.remove('active'); document.body.style.overflow = ''; };

    openBtn?.addEventListener('click', open);
    closeBtn?.addEventListener('click', close);
    cancelBtn?.addEventListener('click', close);
    modal?.addEventListener('click', e => { if (e.target === modal) close(); });
}

// Populate ApptTime select with 30-min slots from 07:00 to 20:30
function populateTimeSelect() {
    const sel = document.getElementById('ApptTime');
    if (!sel) return;
    sel.innerHTML = '<option value="">— Select time —</option>';
    for (let h = HOUR_START; h <= 20; h++) {
        for (let m of [0, 30]) {
            if (h === 20 && m === 30) continue;
            const hh = String(h).padStart(2, '0');
            const mm = String(m).padStart(2, '0');
            const o = document.createElement('option');
            o.value = `${hh}:${mm}`;
            o.textContent = `${hh}:${mm}`;
            sel.appendChild(o);
        }
    }
}

// Compute StartTime and EndTime hidden fields from ApptDate + ApptTime + selected duration
function computeStartEnd() {
    const dateVal = document.getElementById('ApptDate')?.value;
    const timeVal = document.getElementById('ApptTime')?.value;
    const activePill = document.querySelector('#durationPills .cal-duration-pill.active');
    const minutes = activePill ? parseInt(activePill.dataset.minutes, 10) : 30;

    const sHidden = document.getElementById('StartTime');
    const eHidden = document.getElementById('EndTime');

    if (!dateVal || !timeVal) {
        if (sHidden) sHidden.value = '';
        if (eHidden) eHidden.value = '';
        return;
    }

    const start = new Date(`${dateVal}T${timeVal}`);
    const end   = new Date(start.getTime() + minutes * 60000);
    if (sHidden) sHidden.value = formatLocal(start);
    if (eHidden) eHidden.value = formatLocal(end);
}

// Filter ApptTime options: disable past slots when date == today
function applyDateMinima() {
    const dateEl = document.getElementById('ApptDate');
    if (!dateEl) return;
    const today = new Date().toISOString().split('T')[0];
    dateEl.min = today;

    const timeEl = document.getElementById('ApptTime');
    if (!timeEl) return;
    const selectedDate = dateEl.value;
    const nowH = new Date().getHours();
    const nowM = new Date().getMinutes();

    Array.from(timeEl.options).forEach(opt => {
        if (!opt.value) return;
        const [h, m] = opt.value.split(':').map(Number);
        opt.disabled = selectedDate === today && (h < nowH || (h === nowH && m <= nowM));
    });

    // If the currently selected slot is now disabled, reset it
    if (timeEl.selectedOptions[0]?.disabled) {
        timeEl.value = '';
        computeStartEnd();
    }
}

function openCreateModalWithDate(_dateStr, dateObj) {
    if (dateObj < new Date()) {
        showToast('Cannot schedule appointments in the past.', 'error');
        return;
    }
    resetCreateForm();

    const modal = document.getElementById('appointmentModal');
    if (!modal) return;

    // Pre-fill date and round time to next 30-min slot
    const d = new Date(dateObj);
    const dateStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    const h = d.getHours();
    const m = d.getMinutes() < 30 ? 0 : 30;
    const timeStr = `${String(h).padStart(2,'0')}:${String(m).padStart(2,'0')}`;

    const dateEl = document.getElementById('ApptDate');
    const timeEl = document.getElementById('ApptTime');
    if (dateEl) dateEl.value = dateStr;
    if (timeEl) timeEl.value = timeStr;
    applyDateMinima();
    computeStartEnd();

    modal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function resetCreateForm() {
    const existing = document.getElementById('existingPatientRadio');
    if (existing) { existing.checked = true; existing.dispatchEvent(new Event('change')); }

    ['ExistingPatientId', 'StartTime', 'EndTime'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
    // Don't reset DoctorId hidden field for doctors (it's always their own id)
    if (window.currentUser?.role !== 'Doctor') {
        const sel = document.getElementById('DoctorId');
        if (sel) sel.value = '';
    }

    const dateEl = document.getElementById('ApptDate');
    const timeEl = document.getElementById('ApptTime');
    if (dateEl) dateEl.value = '';
    if (timeEl) timeEl.value = '';

    // Reset duration to 30 min default
    document.querySelectorAll('#durationPills .cal-duration-pill').forEach(p => {
        p.classList.toggle('active', p.dataset.minutes === '30');
    });

    document.querySelectorAll('textarea[name^="NewAppointment"]').forEach(t => (t.value = ''));
    document.querySelectorAll('input[name^="NewAppointment.NewPatient"]').forEach(i => (i.value = ''));

    const note = document.getElementById('doctorAvailabilityNote');
    if (note) { note.textContent = ''; note.className = 'cal-doctor-note'; }

    const patNote = document.getElementById('patientConflictNote');
    if (patNote) { patNote.textContent = ''; patNote.className = 'cal-doctor-note'; }
}

// ── Patient toggle ────────────────────────────────────────────
function setupPatientToggle() {
    const existingR = document.getElementById('existingPatientRadio');
    const newR      = document.getElementById('newPatientRadio');
    const existingS = document.getElementById('existing-patient-section');
    const newS      = document.getElementById('new-patient-section');

    existingR?.addEventListener('change', () => { existingS.style.display = 'block'; newS.style.display = 'none'; });
    newR?.addEventListener('change',      () => { existingS.style.display = 'none';  newS.style.display = 'block'; });
}

// ── Doctor availability ───────────────────────────────────────
function setupDoctorAvailability() {
    const dateEl     = document.getElementById('ApptDate');
    const timeEl     = document.getElementById('ApptTime');
    const patientSel = document.getElementById('ExistingPatientId');

    const onTimeChange = function () {
        applyDateMinima();
        computeStartEnd();
        loadAvailableDoctors();
        checkPatientConflict();
    };

    dateEl?.addEventListener('change', onTimeChange);
    timeEl?.addEventListener('change', onTimeChange);
    patientSel?.addEventListener('change', checkPatientConflict);

    // Duration pills
    document.getElementById('durationPills')?.addEventListener('click', e => {
        const pill = e.target.closest('.cal-duration-pill');
        if (!pill) return;
        document.querySelectorAll('#durationPills .cal-duration-pill').forEach(p => p.classList.remove('active'));
        pill.classList.add('active');
        computeStartEnd();
        loadAvailableDoctors();
        checkPatientConflict();
    });
}

function checkPatientConflict(excludeId = null) {
    const patientId = document.getElementById('ExistingPatientId')?.value;
    const start     = document.getElementById('StartTime')?.value;
    const end       = document.getElementById('EndTime')?.value;
    const note      = document.getElementById('patientConflictNote');

    if (!note) return;

    // Only check for existing patient mode
    const isNew = document.getElementById('newPatientRadio')?.checked;
    if (isNew || !patientId || !start || !end) {
        note.textContent = '';
        note.className   = 'cal-doctor-note';
        return;
    }

    let url = `${window.appEndpoints.checkPatientConflict}?patientId=${patientId}&startTime=${encodeURIComponent(start)}&endTime=${encodeURIComponent(end)}`;
    if (excludeId) url += `&excludeId=${excludeId}`;

    fetch(url)
        .then(r => r.json())
        .then(data => {
            if (data.hasConflict) {
                note.textContent = `⚠ ${data.message}`;
                note.className   = 'cal-doctor-note error';
            } else {
                note.textContent = '✓ Patient is free at this time';
                note.className   = 'cal-doctor-note success';
            }
        })
        .catch(() => { note.textContent = ''; note.className = 'cal-doctor-note'; });
}

function loadAvailableDoctors() {
    // Doctors always book for themselves; no need to query availability
    if (window.currentUser?.role === 'Doctor') return;

    const start = document.getElementById('StartTime')?.value;
    const end   = document.getElementById('EndTime')?.value;
    const sel   = document.getElementById('DoctorId');
    const note  = document.getElementById('doctorAvailabilityNote');
    if (!start || !end) return;

    note.textContent = 'Checking availability…';
    note.className   = 'cal-doctor-note loading';
    sel.innerHTML    = '<option value="">Loading…</option>';

    const url = `${window.appEndpoints.getAvailableDoctors}?startTime=${encodeURIComponent(start)}&endTime=${encodeURIComponent(end)}`;

    fetch(url).then(r => r.json()).then(docs => {
        if (!docs.length) {
            sel.innerHTML    = '<option value="">No doctors available</option>';
            note.textContent = 'No doctors free at this time.';
            note.className   = 'cal-doctor-note error';
        } else {
            sel.innerHTML = '<option value="">— Select Doctor —</option>';
            docs.forEach(d => {
                const o = Object.assign(document.createElement('option'), { value: d.value, textContent: d.text });
                sel.appendChild(o);
            });
            note.textContent = `${docs.length} doctor${docs.length > 1 ? 's' : ''} available.`;
            note.className   = 'cal-doctor-note success';
        }
    }).catch(() => { note.textContent = 'Could not load doctors.'; note.className = 'cal-doctor-note error'; });
}

// ── Edit end-time auto-calc ───────────────────────────────────
function setupEditTimeCalc() {
    document.getElementById('EditStartTime')
        ?.addEventListener('change', () => {
            const s = document.getElementById('EditStartTime');
            const e = document.getElementById('EditEndTime');
            if (s?.value) { const d = new Date(s.value); d.setMinutes(d.getMinutes() + 30); e.value = formatLocal(d); }
        });
}


// ── PDF / Excel ────────────────────────────────────────────────
function printWeekPdf() {
    const d = (CAL.view === 'week' ? CAL.weekStart : CAL.focusDate).toISOString().split('T')[0];
    window.open(`/Appointments/PrintWeekSchedulePdf?weekStart=${d}`, '_blank');
}

function exportWeekExcel() {
    const d = (CAL.view === 'week' ? CAL.weekStart : CAL.focusDate).toISOString().split('T')[0];
    window.location.href = `/Appointments/ExportWeekScheduleExcel?weekStart=${d}`;
}

// ── Toast ─────────────────────────────────────────────────────
function showToast(msg, type = 'success') {
    const c = document.getElementById('toastContainer');
    if (!c) return;
    const t = document.createElement('div');
    t.className = `toast ${type}`;
    t.innerHTML = `<span class="toast-msg">${escHtml(msg)}</span>
                   <button class="toast-close" onclick="this.closest('.toast').remove()">✕</button>`;
    c.appendChild(t);
    setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity 0.25s'; setTimeout(() => t.remove(), 260); }, 4500);
}

// ── Utilities ─────────────────────────────────────────────────
function getWeekStart(d) {
    const day = new Date(d);
    const dow = day.getDay();           // 0=Sun .. 6=Sat
    const diff = (dow === 0) ? -6 : 1 - dow;  // Monday = 0
    day.setDate(day.getDate() + diff);
    day.setHours(0, 0, 0, 0);
    return day;
}

function todayMidnight() {
    const d = new Date();
    d.setHours(0, 0, 0, 0);
    return d;
}

function formatLocal(dt) {
    return `${dt.getFullYear()}-${pad(dt.getMonth()+1)}-${pad(dt.getDate())}T${pad(dt.getHours())}:${pad(dt.getMinutes())}`;
}

function pad(n) { return String(n).padStart(2, '0'); }

function escHtml(s) {
    return String(s ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
