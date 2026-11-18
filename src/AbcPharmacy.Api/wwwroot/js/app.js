import { loadSales } from './sales.js';
import { initStock, loadStock } from './stock.js';
import { initDialogs } from './ui.js';

const views = {
  stock: { section: 'view-stock', title: 'Stock', load: loadStock },
  sales: { section: 'view-sales', title: 'Sales', load: loadSales },
};

let isFirstRoute = true;

function route() {
  const requested = location.hash.replace(/^#\//, '');
  const current = views[requested] ? requested : 'stock';

  for (const [name, view] of Object.entries(views)) {
    document.getElementById(view.section).hidden = name !== current;
  }
  document.querySelectorAll('[data-route]').forEach((link) => {
    if (link.dataset.route === current) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  });
  document.title = `${views[current].title} | ABC Pharmacy`;

  if (!isFirstRoute) document.getElementById('main').focus({ preventScroll: true });
  isFirstRoute = false;

  views[current].load();
}

initDialogs();
initStock();
window.addEventListener('hashchange', route);
route();
