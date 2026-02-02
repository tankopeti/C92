// wwwroot/js/Task/calendar.js
(() => {
  'use strict';

  // ============================================================
  // CONFIG
  // ============================================================

  const EVENTS_URL = '/api/tasks/calendar/scheduled';
  const UPDATE_SCHEDULED_URL = (id) =>
    `/api/tasks/${encodeURIComponent(id)}/scheduled-date`;

  // ============================================================
  // STATE
  // ============================================================

  let calendar = null;

  // ============================================================
  // HELPERS
  // ============================================================

  function ensureFullCalendarLoaded() {
    if (typeof window.FullCalendar !== 'undefined') return true;

    console.error(
      '[TaskCalendar] FullCalendar is not defined. ' +
      'Használd a GLOBAL buildet: index.global.min.js'
    );
    return false;
  }

  function getCsrfToken() {
    return (
      document.querySelector('meta[name="csrf-token"]')?.content ||
      document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
      (document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1]
        ? decodeURIComponent(document.cookie.match(/XSRF-TOKEN=([^;]+)/)?.[1])
        : '') ||
      ''
    );
  }

  function toLocalIsoNoZ(d) {
  const pad = (n) => String(n).padStart(2, '0');
  return (
    d.getFullYear() + '-' +
    pad(d.getMonth() + 1) + '-' +
    pad(d.getDate()) + 'T' +
    pad(d.getHours()) + ':' +
    pad(d.getMinutes()) + ':' +
    pad(d.getSeconds())
  );
}


  // ⚠️ Controller DateTime-et vár → JSON STRINGET küldünk
async function updateScheduledDate(taskId, dateObj) {
  const csrf = getCsrfToken();

  const res = await fetch(UPDATE_SCHEDULED_URL(taskId), {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...(csrf ? { RequestVerificationToken: csrf } : {})
    },
    credentials: 'same-origin',
    body: JSON.stringify({
      scheduledDate: toLocalIsoNoZ(dateObj) // ✅ nincs Z, nincs -1 óra
    })
  });

  if (!res.ok) {
    const t = await res.text().catch(() => '');
    throw new Error(`HTTP ${res.status} ${t}`);
  }
}



  function hasViewModalHook() {
    return window.Tasks && typeof window.Tasks.openViewModal === 'function';
  }

  // ============================================================
  // CALENDAR BUILD
  // ============================================================

  function buildCalendar(calendarEl) {
    calendar = new window.FullCalendar.Calendar(calendarEl, {
      // --------------------------------------------------------
      // BASIC
      // --------------------------------------------------------
      locale: 'hu',
      timeZone: 'local',
      height: 'auto',

      // ⚠️ IDŐPONTOS NÉZET
      initialView: 'timeGridWeek',

      // --------------------------------------------------------
      // HEADER
      // --------------------------------------------------------
      headerToolbar: {
        left: 'prev,next today',
        center: 'title',
        right: 'dayGridMonth,timeGridWeek,timeGridDay'
      },

      // --------------------------------------------------------
      // TIME GRID CONFIG
      // --------------------------------------------------------
slotMinTime: '00:00:00',
slotMaxTime: '24:00:00',
scrollTime: '08:00:00', // csak oda görget alapból, de a nap teljes

slotDuration: '00:15:00',
snapDuration: '00:15:00',



      allDaySlot: true,     // legyen all-day sáv
      defaultAllDay: false, // eventek alapból NEM all-day-ek

      slotLabelFormat: {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
      },

      eventTimeFormat: {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
      },

      // --------------------------------------------------------
      // DATA
      // --------------------------------------------------------
      events: EVENTS_URL,

      // --------------------------------------------------------
      // INTERACTION (INGYENES)
      // --------------------------------------------------------
      editable: true,
      eventStartEditable: true,
      eventDurationEditable: true,

      // --------------------------------------------------------
      // CLICK → TASK VIEW MODAL
      // --------------------------------------------------------
      eventClick: function (info) {
        info.jsEvent?.preventDefault();

        const taskId = info?.event?.id;
        if (!taskId) return;

        if (hasViewModalHook()) {
          window.Tasks.openViewModal(taskId);
        } else {
          console.warn('[TaskCalendar] openViewModal nem elérhető');
        }
      },

      // --------------------------------------------------------
      // DRAG & DROP → AJAX SAVE
      // --------------------------------------------------------
      eventDrop: function (info) {
        const taskId = info?.event?.id;
        const newStart = info?.event?.start; // Date → óra/perc benne!

        if (!taskId || !newStart) return;

        updateScheduledDate(taskId, newStart)
          .then(() => calendar.refetchEvents())
.catch((err) => {
  console.error('[TaskCalendar] save failed', err);
  alert('Mentés hiba: ' + (err?.message || err));
  info.revert();
});

      },

      // --------------------------------------------------------
      // RESIZE → AJAX SAVE
      // (ScheduledDate van → start mentése)
      // --------------------------------------------------------
      eventResize: function (info) {
        const taskId = info?.event?.id;
        const newStart = info?.event?.start;

        if (!taskId || !newStart) return;

        updateScheduledDate(taskId, newStart)
          .then(() => calendar.refetchEvents())
.catch((err) => {
  console.error('[TaskCalendar] save failed', err);
  alert('Mentés hiba: ' + (err?.message || err));
  info.revert();
});

      },

      // --------------------------------------------------------
      // DEBUG: ha allDay event jön, látod a konzolban
      // --------------------------------------------------------
      eventDidMount: function (arg) {
        if (arg.event.allDay) {
          console.warn(
            '[TaskCalendar] allDay event érkezett – időpontra húzás nem fog menni:',
            arg.event.id
          );
        }
      }
    });
  }

  // ============================================================
  // OPEN / REFRESH
  // ============================================================

  function openOrRefreshCalendar() {
    if (!ensureFullCalendarLoaded()) return;

    const calendarEl = document.getElementById('fullCalendar');
    if (!calendarEl) return;

    if (!calendar) {
      buildCalendar(calendarEl);
      calendar.render();
      calendar.refetchEvents();
      console.log('[TaskCalendar] rendered (first open)');
    } else {
      calendar.updateSize();
      calendar.refetchEvents();
      console.log('[TaskCalendar] refreshed on open');
    }
  }

  // ============================================================
  // BIND MODAL
  // ============================================================

  function bindModal() {
    const modal = document.getElementById('calendarModal');
    if (!modal) return;

    // ✅ MINDEN megnyitáskor frissít
    modal.addEventListener('shown.bs.modal', openOrRefreshCalendar);
  }

  document.addEventListener('DOMContentLoaded', bindModal);
})();
