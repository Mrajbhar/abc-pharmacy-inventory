
const ESCAPES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };

export const escapeHtml = (value) => String(value ?? '').replace(/[&<>"']/g, (c) => ESCAPES[c]);

let toastTimer;

export function showToast(message, { error = false } = {}) {
  const toast = document.getElementById('toast');
  toast.textContent = message;
  toast.classList.toggle('is-error', error);
  toast.hidden = false;
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => {
    toast.hidden = true;
  }, 4000);
}

export function initDialogs() {
  document.querySelectorAll('dialog').forEach((dialog) => {
    dialog.querySelectorAll('[data-close]').forEach((button) =>
      button.addEventListener('click', () => dialog.close()),
    );
    dialog.addEventListener('click', (event) => {
      if (event.target === dialog) dialog.close();
    });
    dialog.querySelector('form')?.addEventListener('input', (event) => clearFieldError(event.target));
  });
}

function clearFieldError(input) {
  if (!input.name || !input.hasAttribute('aria-invalid')) return;
  input.removeAttribute('aria-invalid');
  const slot = input.form.querySelector(`[data-error-for="${input.name}"]`);
  if (slot) slot.textContent = '';
}

export function clearFormErrors(form) {
  form.querySelectorAll('[data-error-for]').forEach((el) => {
    el.textContent = '';
  });
  form.querySelectorAll('[aria-invalid]').forEach((el) => el.removeAttribute('aria-invalid'));
  const formError = form.querySelector('[data-form-error]');
  formError.hidden = true;
  formError.textContent = '';
}

/**
 * Show validation messages next to their fields.
 * @param {HTMLFormElement} form
 * @param {Record<string, string|string[]>} errors keyed by field name
 * @param {string} [fallbackMessage] shown at the top of the form for errors that match no field
 */
export function showFormErrors(form, errors, fallbackMessage) {
  const unmatched = [];
  let firstInvalid = null;

  for (const [field, messages] of Object.entries(errors)) {
    const text = [].concat(messages).join(' ');
    const slot = form.querySelector(`[data-error-for="${field}"]`);
    const input = form.elements.namedItem(field);
    if (slot && input) {
      slot.textContent = text;
      input.setAttribute('aria-invalid', 'true');
      input.setAttribute('aria-describedby', slot.id || assignId(slot, field));
      firstInvalid ??= input;
    } else if (field !== 'request') {
      unmatched.push(text);
    }
  }

  const message = unmatched.length ? unmatched.join(' ') : !firstInvalid ? fallbackMessage : '';
  if (message) {
    const formError = form.querySelector('[data-form-error]');
    formError.textContent = message;
    formError.hidden = false;
  }

  firstInvalid?.focus();
}

function assignId(element, field) {
  element.id = `${element.closest('form').id}-${field}-error`;
  return element.id;
}

export function setBusy(button, busy) {
  button.disabled = busy;
  button.setAttribute('aria-busy', String(busy));
}

export function debounce(fn, ms) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), ms);
  };
}
