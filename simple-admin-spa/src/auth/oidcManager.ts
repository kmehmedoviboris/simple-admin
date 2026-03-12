import { UserManager, WebStorageStateStore } from 'oidc-client-ts'

export const userManager = new UserManager({
  authority: import.meta.env.VITE_API_BASE_URL,
  client_id: 'simple-admin-spa',
  redirect_uri: `${import.meta.env.VITE_SPA_BASE_URL}/callback`,
  post_logout_redirect_uri: `${import.meta.env.VITE_SPA_BASE_URL}/`,
  response_type: 'code',
  scope: 'openid email profile api',
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
  automaticSilentRenew: false,
  loadUserInfo: false,
})
