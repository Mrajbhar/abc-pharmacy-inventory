import { api } from './api.js';
import { formatDateTime, formatMoney, plural } from './format.js';
import { escapeHtml } from './ui.js';

const $ = (id) => document.getElementById(id);

export async function loadSales() {
  try {
    render(await api.getSales());
  } catch (error) {
    render([]);
    showEmpty(`<strong>Sales could not be loaded</strong>${escapeHtml(error.message)}`);
  }
}

function render(sales) {
  const totalCents = sales.reduce((sum, s) => sum + Math.round(s.totalAmount * 100), 0);
  const units = sales.reduce((sum, s) => sum + s.quantity, 0);

  $('sales-summary').textContent = sales.length
    ? `${plural(sales.length, 'sale')}, ${plural(units, 'unit')} sold, ${formatMoney(totalCents / 100)} in total`
    : '';

  $('sales-rows').innerHTML = sales
    .map(
      (s) => `
      <tr>
        <td class="nowrap">${formatDateTime(s.soldAtUtc)}</td>
        <td><strong>${escapeHtml(s.medicineName)}</strong></td>
        <td>${escapeHtml(s.brand)}</td>
        <td>${s.customerName ? escapeHtml(s.customerName) : '<span class="optional">Not given</span>'}</td>
        <td class="num">${s.quantity}</td>
        <td class="num">${formatMoney(s.unitPrice)}</td>
        <td class="num"><strong>${formatMoney(s.totalAmount)}</strong></td>
      </tr>`,
    )
    .join('');

  if (sales.length) {
    $('sales-empty').hidden = true;
  } else {
    showEmpty('<strong>No sales yet</strong>Sales appear here after you sell a medicine from the <a href="#/stock">Stock</a> page.');
  }
}

function showEmpty(html) {
  const empty = $('sales-empty');
  empty.innerHTML = html;
  empty.hidden = false;
}
