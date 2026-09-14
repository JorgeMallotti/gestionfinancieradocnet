export const environment = {
  production: true,
  // Production: public API base URL only — no secrets here.
  // The backend lives on a subdomain of the SAME registrable domain as the frontend
  // (finanzas.* / api.*). This is mandatory, not cosmetic: the refresh token is an
  // httpOnly cookie with SameSite=Strict, and such a cookie is never sent on
  // cross-site requests (e.g. frontend on *.azurestaticapps.net → backend on *.azurewebsites.net).
  // Keeping both on mallottidigital.com makes every request same-site, so the cookie travels.
  apiBaseUrl: 'https://api.mallottidigital.com',
  // Jorge's landing page — toolbar link to navigate landing ↔ MVP (AGENTS.md §14)
  landingUrl: 'https://www.mallottidigital.com',
};
