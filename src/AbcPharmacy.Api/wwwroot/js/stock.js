import { api, ApiError } from './api.js';
import { describeExpiry, formatDate, formatMoney, plural, todayIso } from './format.js';
import { clearFormErrors, debounce, escapeHtml, setBusy, showFormErrors, showToast } from './ui.js';

const state = {
  medicines: [],
  search: '',
  filter: 'all', 
  requestId: 0,
  highlightId: null, 
  sellingId: null,
};

const $ = (id) => document.getElementById(id);

export function initStock() {
  const searchInput = $('search');
  const runSearch = debounce(() => {
    state.search = searchInput.value.trim();
    loadStock();
  }, 250);
  searchInput.addEventListener('input', runSearch);

  document.querySelectorAll('[data-filter]').forEach((button) =>
    button.addEventListener('click', () => {
      state.filter = button.dataset.filter;
      render();
    }),
  );

  $('stock-rows').addEventListener('click', (event) => {
    const sell = event.target.closest('[data-sell]');
    if (sell) openSell(sell.dataset.sell);
    const details = event.target.closest('[data-details]');
    if (details) openDetails(details.dataset.details);
  });

  $('open-add').addEventListener('click', openAdd);
  $('add-form').addEventListener('submit', submitAdd);
  $('sell-form').addEventListener('submit', submitSell);
  $('sell-quantity').addEventListener('input', updateSellTotal);
}

export async function loadStock() {
  const requestId = ++state.requestId;
  try {
    const medicines = await api.getMedicines(state.search);
    if (requestId !== state.requestId) return; 
    state.medicines = medicines;
    render();
  } catch (error) {
    if (requestId !== state.requestId) return;
    state.medicines = [];
    render();
    showEmpty(`<strong>Medicines could not be loaded</strong>${escapeHtml(error.message)}`);
  }
}


const isVisible = (m) =>
  state.filter === 'all' || (state.filter === 'expiring' ? m.isExpiringSoon : m.isLowStock);

function render() {
  const { medicines } = state;
  const counts = {
    all: medicines.length,
    expiring: medicines.filter((m) => m.isExpiringSoon).length,
    low: medicines.filter((m) => m.isLowStock).length,
  };

  document.querySelectorAll('[data-count]').forEach((el) => {
    el.textContent = counts[el.dataset.count];
  });
  document.querySelectorAll('[data-filter]').forEach((button) =>
    button.setAttribute('aria-pressed', String(button.dataset.filter === state.filter)),
  );

  $('stock-summary').textContent = state.search
    ? `${plural(medicines.length, 'medicine')} ${medicines.length === 1 ? 'matches' : 'match'} “${state.search}”`
    : `${plural(medicines.length, 'medicine')} in stock`;

  const visible = medicines.filter(isVisible);
  $('stock-rows').innerHTML = visible.map(rowHtml).join('');

  if (visible.length) {
    $('stock-empty').hidden = true;
  } else if (state.search) {
    showEmpty(`<strong>No medicines match “${escapeHtml(state.search)}”</strong>Check the spelling, or clear the search to see all stock.`);
  } else if (state.filter !== 'all') {
    showEmpty(`<strong>Nothing to flag here</strong>No medicines are ${state.filter === 'expiring' ? 'close to expiry' : 'running low'}.`);
  } else {
    showEmpty('<strong>No medicines yet</strong>Use “Add medicine” to record your first item of stock.');
  }

  flashNewRow();
}

function rowHtml(m) {
  const status = m.isExpiringSoon ? 'is-expiring' : m.isLowStock ? 'is-low' : '';
  const id = escapeHtml(m.id);
  const name = escapeHtml(m.fullName);

  let action;
  if (m.isExpired) action = '<span class="cell-sub">Expired</span>';
  else if (m.quantity === 0) action = '<span class="cell-sub">Out of stock</span>';
  else action = `<button type="button" class="btn btn-sell" data-sell="${id}" aria-label="Sell ${name}">Sell</button>`;

  return `
    <tr class="${status}" data-row="${id}">
      <td><button type="button" class="med-name" data-details="${id}" title="Show details and notes">${name}</button></td>
      <td>${escapeHtml(m.brand)}</td>
      <td class="nowrap">
        ${formatDate(m.expiryDate)}
        ${m.isExpiringSoon ? `<span class="cell-sub expiry">${describeExpiry(m.daysToExpiry)}</span>` : ''}
      </td>
      <td class="num nowrap">${m.isLowStock ? '<span class="tag tag-low">Low stock</span>' : ''}<span class="qty">${m.quantity}</span></td>
      <td class="num">${formatMoney(m.price)}</td>
      <td class="actions">${action}</td>
    </tr>`;
}

function showEmpty(html) {
  const empty = $('stock-empty');
  empty.innerHTML = html;
  empty.hidden = false;
}

function flashNewRow() {
  if (!state.highlightId) return;
  const row = document.querySelector(`[data-row="${CSS.escape(state.highlightId)}"]`);
  state.highlightId = null;
  if (!row) return;
  row.classList.add('is-new');
  row.scrollIntoView({ block: 'nearest' });
  row.querySelector('.med-name')?.focus({ preventScroll: true });
}

const findMedicine = (id) => state.medicines.find((m) => m.id === id);


function openAdd() {
  const form = $('add-form');
  form.reset();
  clearFormErrors(form);
  form.elements.expiryDate.min = todayIso();
  $('add-dialog').showModal();
  form.elements.fullName.focus();
}

function validateAdd(form) {
  const v = Object.fromEntries(new FormData(form));
  const errors = {};

  const fullName = v.fullName.trim();
  if (!fullName) errors.fullName = 'Enter the medicine’s full name.';
  else if (fullName.length < 2) errors.fullName = 'Full name must be at least 2 characters.';

  if (!v.brand.trim()) errors.brand = 'Enter the brand.';

  if (!v.expiryDate) errors.expiryDate = 'Choose an expiry date.';
  else if (v.expiryDate < todayIso()) errors.expiryDate = 'Expiry date cannot be in the past.';

  if (v.quantity === '') errors.quantity = 'Enter the quantity in stock.';
  else if (!/^\d+$/.test(v.quantity)) errors.quantity = 'Quantity must be a whole number of 0 or more.';
  else if (Number(v.quantity) > 1_000_000) errors.quantity = 'Quantity cannot be more than 1,000,000.';

  if (v.price === '') errors.price = 'Enter the price per unit.';
  else if (!/^\d+(\.\d{1,2})?$/.test(v.price)) errors.price = 'Price must be a number with up to 2 decimal places.';
  else if (Number(v.price) <= 0) errors.price = 'Price must be more than 0.';
  else if (Number(v.price) > 1_000_000) errors.price = 'Price cannot be more than 1,000,000.';

  return { errors, values: { ...v, fullName } };
}

async function submitAdd(event) {
  event.preventDefault();
  const form = event.currentTarget;
  clearFormErrors(form);

  const { errors, values } = validateAdd(form);
  if (Object.keys(errors).length) {
    showFormErrors(form, errors);
    return;
  }

  const submit = form.querySelector('[type="submit"]');
  setBusy(submit, true);
  try {
    const created = await api.addMedicine({
      fullName: values.fullName,
      brand: values.brand.trim(),
      expiryDate: values.expiryDate,
      quantity: Number(values.quantity),
      price: Number(values.price),
      notes: values.notes.trim() || null,
    });
    $('add-dialog').close();
    showToast(`Medicine added: ${created.fullName}`);

    $('search').value = '';
    state.search = '';
    state.filter = 'all';
    state.highlightId = created.id;
    await loadStock();
  } catch (error) {
    showFormErrors(form, error instanceof ApiError ? error.fieldErrors : {}, error.message);
  } finally {
    setBusy(submit, false);
  }
}


function openSell(id) {
  const medicine = findMedicine(id);
  if (!medicine) return;
  state.sellingId = id;

  const form = $('sell-form');
  form.reset();
  clearFormErrors(form);
  $('sell-name').textContent = medicine.fullName;
  $('sell-stock').textContent = `${medicine.quantity} in stock at ${formatMoney(medicine.price)} each`;
  form.elements.quantity.max = medicine.quantity;
  form.elements.quantity.value = 1;
  updateSellTotal();

  $('sell-dialog').showModal();
  form.elements.quantity.select();
}

function updateSellTotal() {
  const medicine = findMedicine(state.sellingId);
  const quantity = Number($('sell-quantity').value);
  const valid = medicine && Number.isInteger(quantity) && quantity > 0;
  $('sell-total').textContent = valid ? formatMoney(Math.round(medicine.price * 100) * quantity / 100) : '–';
}

async function submitSell(event) {
  event.preventDefault();
  const form = event.currentTarget;
  clearFormErrors(form);

  const medicine = findMedicine(state.sellingId);
  const raw = form.elements.quantity.value;
  const quantity = Number(raw);
  const customerName = form.elements.customerName.value.trim();

  if (!/^\d+$/.test(raw) || quantity < 1) {
    showFormErrors(form, { quantity: 'Enter a whole number of 1 or more.' });
    return;
  }
  if (quantity > medicine.quantity) {
    showFormErrors(form, { quantity: `Only ${medicine.quantity} in stock.` });
    return;
  }

  const submit = form.querySelector('[type="submit"]');
  setBusy(submit, true);
  try {
    const sale = await api.recordSale({ medicineId: medicine.id, quantity, customerName: customerName || null });
    $('sell-dialog').close();
    showToast(`Sale recorded: ${sale.quantity} × ${sale.medicineName} for ${formatMoney(sale.totalAmount)}`);
    await loadStock();
  } catch (error) {
    showFormErrors(form, error instanceof ApiError ? error.fieldErrors : {}, error.message);
    if (error instanceof ApiError && error.status === 422) loadStock();
  } finally {
    setBusy(submit, false);
  }
}


function openDetails(id) {
  const m = findMedicine(id);
  if (!m) return;

  $('details-title').textContent = m.fullName;
  const rows = [
    ['Brand', escapeHtml(m.brand)],
    ['Expiry date', `${formatDate(m.expiryDate)} (${describeExpiry(m.daysToExpiry).toLowerCase()})`],
    ['Quantity', `${m.quantity}${m.isLowStock ? ', low stock' : ''}`],
    ['Price', formatMoney(m.price)],
    ['Notes', m.notes ? escapeHtml(m.notes) : '<span class="optional">No notes</span>'],
  ];
  $('details-list').innerHTML = rows.map(([label, value]) => `<dt>${label}</dt><dd>${value}</dd>`).join('');
  $('details-dialog').showModal();
}
