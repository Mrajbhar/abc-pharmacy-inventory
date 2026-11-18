

export class ApiError extends Error {
  /**
   * @param {number} status HTTP status (0 when the server could not be reached)
   * @param {object|null} problem RFC 7807 ProblemDetails body returned by the API
   */
  constructor(status, problem) {
    super(problem?.detail || problem?.title || `Request failed (${status}).`);
    this.status = status;
    this.fieldErrors = normaliseFieldErrors(problem?.errors);
  }
}

function normaliseFieldErrors(errors) {
  const result = {};
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const field = key.replace(/^\$\./, '');
    const camel = field.charAt(0).toLowerCase() + field.slice(1);
    result[camel] = messages.map((m) =>
      m.startsWith('The JSON value could not be converted') ? 'Enter a valid value.' : m,
    );
  }
  return result;
}

async function request(path, { method = 'GET', body } = {}) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, { detail: 'Cannot reach the server. Check that the API is running, then try again.' });
  }

  const isJson = response.headers.get('content-type')?.includes('json');
  const data = isJson ? await response.json() : null;
  if (!response.ok) throw new ApiError(response.status, data);
  return data;
}

export const api = {
  getMedicines: (search = '') =>
    request(search ? `api/medicines?search=${encodeURIComponent(search)}` : 'api/medicines'),
  getMedicine: (id) => request(`api/medicines/${encodeURIComponent(id)}`),
  addMedicine: (medicine) => request('api/medicines', { method: 'POST', body: medicine }),
  getSales: () => request('api/sales'),
  recordSale: (sale) => request('api/sales', { method: 'POST', body: sale }),
};
