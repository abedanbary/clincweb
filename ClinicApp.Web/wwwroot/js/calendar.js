// calendar.js
let currentAppointmentId = null;
let isEditMode = false;

document.addEventListener('DOMContentLoaded', function() {
    initializeCalendar();
    setupCreateModal();
    setupPatientToggle();
});

// Initialize FullCalendar
function initializeCalendar() {
    var calendarEl = document.getElementById('calendar');
    
    if (!calendarEl) return; // If not on calendar page, exit
    
    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        buttonText: {
            today: 'Today',
            month: 'Month',
            week: 'Week',
            day: 'Day'
        },
        height: 'auto',
        slotMinTime: '07:00:00',
        slotMaxTime: '22:00:00',
        slotDuration: '00:30:00',
        selectable: true,
        
        events: '/Appointments/GetCalendarEvents',
        
        eventClick: function(info) {
            showAppointmentDetails(info.event);
        },
        
        dateClick: function(info) {
            openCreateModalWithDate(info.dateStr, info.date);
        },
        
        select: function(info) {
            openCreateModalWithDateRange(info.start, info.end);
        },
        
        eventTimeFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        },
        slotLabelFormat: {
            hour: '2-digit',
            minute: '2-digit',
            hour12: false
        }
    });
    
    calendar.render();
    
    // Store calendar instance globally for refresh
    window.calendarInstance = calendar;
}





// Show appointment details (View Mode)
function showAppointmentDetails(event) {
    currentAppointmentId = event.id;
    isEditMode = false;
    
    const props = event.extendedProps;
    const viewMode = document.getElementById('viewMode');
    const editMode = document.getElementById('editMode');
    const modalTitle = document.getElementById('modalTitle');
    const detailsActions = document.getElementById('detailsActions');
    
    const statusClass = {
        'Scheduled': 'status-scheduled',
        'Completed': 'status-completed',
        'Cancelled': 'status-cancelled',
        'NoShow': 'status-noshow'
    }[props.status] || 'status-scheduled';

    viewMode.style.display = 'block';
    editMode.style.display = 'none';
    modalTitle.textContent = 'Appointment Details';

    viewMode.innerHTML = `
        <div class="appointment-detail-row">
            
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Patient</div>
                <div class="appointment-detail-value">${props.patientName}</div>
            </div>
        </div>
        <div class="appointment-detail-row">
            
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Phone</div>
                <div class="appointment-detail-value">${props.patientPhone}</div>
            </div>
        </div>
        <div class="appointment-detail-row">
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Doctor</div>
                <div class="appointment-detail-value">${props.doctorName}</div>
            </div>
        </div>
        <div class="appointment-detail-row">
        
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Date & Time</div>
                <div class="appointment-detail-value">
                    ${event.start.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}<br>
                    ${event.start.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })} - ${event.end.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}
                </div>
            </div>
        </div>
        ${props.reason ? `
        <div class="appointment-detail-row">
           
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Reason</div>
                <div class="appointment-detail-value">${props.reason}</div>
            </div>
        </div>
        ` : ''}
        <div class="appointment-detail-row">
           
            <div class="appointment-detail-content">
                <div class="appointment-detail-label">Status</div>
                <div class="appointment-status-badge ${statusClass}">${props.status}</div>
            </div>
        </div>
    `;

    detailsActions.innerHTML = `
        <button type="button" class="btn-edit" onclick="switchToEditMode()">
             Edit
        </button>
        <button type="button" class="btn-delete" onclick="deleteAppointment(${event.id})">
             Delete
        </button>
    `;

    document.getElementById('detailsModal').classList.add('active');
}

// Switch to Edit Mode
// Switch to Edit Mode
function switchToEditMode() {
    isEditMode = true;
    
    fetch('/Appointments/GetAppointment/' + currentAppointmentId)
        .then(response => response.json())
        .then(data => {
            const viewMode = document.getElementById('viewMode');
            const editMode = document.getElementById('editMode');
            const modalTitle = document.getElementById('modalTitle');
            const detailsActions = document.getElementById('detailsActions');

            viewMode.style.display = 'none';
            editMode.style.display = 'block';
            modalTitle.textContent = 'Edit Appointment';

            // Fill form with data
            document.getElementById('EditId').value = data.id;
            document.getElementById('EditPatientName').value = data.patientName;
            document.getElementById('EditDoctorId').value = data.doctorId;
            document.getElementById('EditStartTime').value = data.startTime;
            document.getElementById('EditEndTime').value = data.endTime;
            document.getElementById('EditStatus').value = data.status;
            document.getElementById('EditReasonForVisit').value = data.reasonForVisit || '';
            document.getElementById('EditNotes').value = data.notes || '';

            detailsActions.innerHTML = `
                <button type="button" class="btn-ghost" onclick="cancelEdit()">
                    Cancel
                </button>
                <button type="submit" form="editForm" class="btn-primary">
                    💾 Save Changes
                </button>
            `;
        })
        .catch(err => {
            alert('Error loading appointment data');
            console.error(err);
        });
}

// Cancel Edit
function cancelEdit() {
    if (window.calendarInstance) {
        window.calendarInstance.refetchEvents();
    }
    closeDetailsModal();
}

// Close details modal
function closeDetailsModal() {
    document.getElementById('detailsModal').classList.remove('active');
    isEditMode = false;
}

// Delete appointment
function deleteAppointment(id) {
    if (confirm('Are you sure you want to delete this appointment?')) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = '/Appointments/Delete/' + id;
        
        const token = document.createElement('input');
        token.type = 'hidden';
        token.name = '__RequestVerificationToken';
        token.value = document.querySelector('input[name="__RequestVerificationToken"]').value;
        
        form.appendChild(token);
        document.body.appendChild(form);
        form.submit();
    }
}

// Setup details modal close
document.addEventListener('DOMContentLoaded', function() {
    const closeDetailsBtn = document.getElementById('closeDetailsBtn');
    const detailsModal = document.getElementById('detailsModal');
    
    if (closeDetailsBtn) {
        closeDetailsBtn.addEventListener('click', closeDetailsModal);
    }
    
    if (detailsModal) {
        detailsModal.addEventListener('click', function(e) {
            if (e.target === this) {
                closeDetailsModal();
            }
        });
    }
});

// Open create modal with specific date
function openCreateModalWithDate(dateStr, dateObj) {
    resetCreateForm();
    
    const modal = document.getElementById('appointmentModal');
    const startTimeInput = document.getElementById('StartTime');
    const endTimeInput = document.getElementById('EndTime');
    
    const defaultDate = new Date(dateObj);
    defaultDate.setHours(9, 0, 0, 0);
    
    const year = defaultDate.getFullYear();
    const month = String(defaultDate.getMonth() + 1).padStart(2, '0');
    const day = String(defaultDate.getDate()).padStart(2, '0');
    
    startTimeInput.value = `${year}-${month}-${day}T09:00`;
    
    const endDate = new Date(defaultDate);
    endDate.setMinutes(endDate.getMinutes() + 30);
    const endHours = String(endDate.getHours()).padStart(2, '0');
    const endMinutes = String(endDate.getMinutes()).padStart(2, '0');
    endTimeInput.value = `${year}-${month}-${day}T${endHours}:${endMinutes}`;
    
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

// Open create modal with date range
function openCreateModalWithDateRange(startDate, endDate) {
    resetCreateForm();
    
    const modal = document.getElementById('appointmentModal');
    const startTimeInput = document.getElementById('StartTime');
    const endTimeInput = document.getElementById('EndTime');
    
    const startYear = startDate.getFullYear();
    const startMonth = String(startDate.getMonth() + 1).padStart(2, '0');
    const startDay = String(startDate.getDate()).padStart(2, '0');
    const startHours = String(startDate.getHours()).padStart(2, '0');
    const startMinutes = String(startDate.getMinutes()).padStart(2, '0');
    
    startTimeInput.value = `${startYear}-${startMonth}-${startDay}T${startHours}:${startMinutes}`;
    
    const endYear = endDate.getFullYear();
    const endMonth = String(endDate.getMonth() + 1).padStart(2, '0');
    const endDay = String(endDate.getDate()).padStart(2, '0');
    const endHours = String(endDate.getHours()).padStart(2, '0');
    const endMinutes = String(endDate.getMinutes()).padStart(2, '0');
    
    endTimeInput.value = `${endYear}-${endMonth}-${endDay}T${endHours}:${endMinutes}`;
    
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
}

// Reset create form
function resetCreateForm() {
    const existingRadio = document.getElementById('existingPatientRadio');
    if (existingRadio) {
        existingRadio.checked = true;
        existingRadio.dispatchEvent(new Event('change'));
    }
    
    const fields = [
        'ExistingPatientId',
        'DoctorId',
        'StartTime',
        'EndTime'
    ];
    
    fields.forEach(id => {
        const element = document.getElementById(id);
        if (element) element.value = '';
    });
    
    const textareas = document.querySelectorAll('textarea[name^="NewAppointment"]');
    textareas.forEach(textarea => textarea.value = '');
    
    const inputs = document.querySelectorAll('input[name^="NewAppointment.NewPatient"]');
    inputs.forEach(input => input.value = '');
}

// Setup create modal
function setupCreateModal() {
    const modal = document.getElementById('appointmentModal');
    const openBtn = document.getElementById('openModalBtn');
    const closeBtn = document.getElementById('closeModalBtn');
    const cancelBtn = document.getElementById('cancelModalBtn');
    
    if (openBtn) {
        openBtn.addEventListener('click', function() {
            resetCreateForm();
            modal.style.display = 'flex';
            document.body.style.overflow = 'hidden';
        });
    }
    
    function closeCreateModal() {
        modal.style.display = 'none';
        document.body.style.overflow = '';
    }
    
    if (closeBtn) closeBtn.addEventListener('click', closeCreateModal);
    if (cancelBtn) cancelBtn.addEventListener('click', closeCreateModal);
    
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                closeCreateModal();
            }
        });
    }
}

// Setup patient toggle
function setupPatientToggle() {
    const existingRadio = document.getElementById('existingPatientRadio');
    const newRadio = document.getElementById('newPatientRadio');
    const existingSection = document.getElementById('existing-patient-section');
    const newSection = document.getElementById('new-patient-section');
    
    if (existingRadio) {
        existingRadio.addEventListener('change', function() {
            if (this.checked) {
                existingSection.style.display = 'block';
                newSection.style.display = 'none';
            }
        });
    }
    
    if (newRadio) {
        newRadio.addEventListener('change', function() {
            if (this.checked) {
                existingSection.style.display = 'none';
                newSection.style.display = 'block';
            }
        });
    }
}

// Auto-calculate end time
document.addEventListener('DOMContentLoaded', function() {
    const startTimeInput = document.getElementById('StartTime');
    const editStartTimeInput = document.getElementById('EditStartTime');
    
    if (startTimeInput) {
        startTimeInput.addEventListener('change', function() {
            calculateEndTime('StartTime', 'EndTime');
        });
    }
    
    if (editStartTimeInput) {
        editStartTimeInput.addEventListener('change', function() {
            calculateEndTime('EditStartTime', 'EditEndTime');
        });
    }
});

function calculateEndTime(startId, endId) {
    const startInput = document.getElementById(startId);
    const endInput = document.getElementById(endId);
    
    if (startInput && endInput && startInput.value) {
        const startDate = new Date(startInput.value);
        startDate.setMinutes(startDate.getMinutes() + 30);

        const year = startDate.getFullYear();
        const month = String(startDate.getMonth() + 1).padStart(2, '0');
        const day = String(startDate.getDate()).padStart(2, '0');
        const hours = String(startDate.getHours()).padStart(2, '0');
        const minutes = String(startDate.getMinutes()).padStart(2, '0');

        endInput.value = `${year}-${month}-${day}T${hours}:${minutes}`;
    }
    // ... كل الكود الموجود ...
}
// Print week as PDF (أضف في الآخر)
     function printWeekPdf() {
       const calendar = window.calendarInstance;
    
       if (!calendar) {
            alert('Calendar not initialized');
         return;
        }
    
        // Get current date
      const currentDate = calendar.getDate();
      const dateStr = currentDate.toISOString().split('T')[0];
    
      // Open PDF in new tab
      window.open(`/Appointments/PrintWeekSchedulePdf?weekStart=${dateStr}`, '_blank');
    }
    // Export week as Excel
function exportWeekExcel() {
    const calendar = window.calendarInstance;
    
    if (!calendar) {
        alert('Calendar not initialized');
        return;
    }
    
    const currentDate = calendar.getDate();
    const dateStr = currentDate.toISOString().split('T')[0];
    
    // Download Excel
    window.location.href = `/Appointments/ExportWeekScheduleExcel?weekStart=${dateStr}`;
}

