const LOCALE = 'en-IN';
const CURRENCY = 'INR';

const moneyFormat = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: CURRENCY,
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export const formatMoney = (amount) => moneyFormat.format(amount);

export function formatDate(isoDate) {
  const [year, month, day] = isoDate.split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString(LOCALE, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export const formatDateTime = (isoDateTime) =>
  new Date(isoDateTime).toLocaleString(LOCALE, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });

const plural = (n, word) => `${n} ${word}${n === 1 ? '' : 's'}`;

export function describeExpiry(daysToExpiry) {
  if (daysToExpiry < 0) return `Expired ${plural(-daysToExpiry, 'day')} ago`;
  if (daysToExpiry === 0) return 'Expires today';
  return `${plural(daysToExpiry, 'day')} left`;
}

export function todayIso() {
  const now = new Date();
  const pad = (n) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

export { plural };
