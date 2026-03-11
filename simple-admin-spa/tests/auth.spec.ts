import { test, expect, type Page } from '@playwright/test'

/**
 * Helper: navigate to /users (triggers OIDC redirect), fill credentials, submit, wait for /users.
 */
async function loginAsAdmin(page: Page) {
  await page.goto('/users')
  // Wait for redirect to backend login page
  await page.waitForURL('**/Account/Login**', { timeout: 15000 })
  // Fill in credentials using the Razor form field ids
  await page.fill('#Email', 'admin@simpleadmin.local')
  await page.fill('#Password', 'Admin1234!')
  await page.click('button[type="submit"]')
  // After login, OpenIddict redirects to /callback which redirects to /users
  await page.waitForURL('**/users', { timeout: 15000 })
}

test.describe('Auth flow (AUTH-02)', () => {
  test('unauthenticated redirect', async ({ page }) => {
    await page.goto('/users')
    // Should be redirected to the Razor login page on the backend
    await page.waitForURL('**/Account/Login**', { timeout: 15000 })
    expect(page.url()).toContain('/Account/Login')
  })

  test('login flow', async ({ page }) => {
    await loginAsAdmin(page)
    // Should land on the SPA /users route
    expect(page.url()).toContain('/users')
  })

  test('session persistence', async ({ page }) => {
    // Login first
    await loginAsAdmin(page)
    // Reload the page — should stay on /users without being redirected to login
    await page.reload()
    // Wait a moment for any potential redirect
    await page.waitForTimeout(2000)
    expect(page.url()).toContain('/users')
    // Should NOT be on login page
    expect(page.url()).not.toContain('/Account/Login')
  })

  test('logout', async ({ page }) => {
    // Login first
    await loginAsAdmin(page)
    // Click the Logout button in the AppShell toolbar
    await page.click('button:has-text("Logout")')
    // Should be redirected away from /users
    await page.waitForURL((url) => !url.toString().includes('/users') || url.toString().includes('/Account/Login'), { timeout: 15000 })
    // URL should no longer be the SPA /users page (redirected to login or SPA root)
    expect(page.url()).not.toMatch(/localhost:5173\/users$/)
  })

  test('protected route after logout', async ({ page }) => {
    // Login first
    await loginAsAdmin(page)
    // Logout
    await page.click('button:has-text("Logout")')
    await page.waitForTimeout(2000)
    // Try to navigate to /users again
    await page.goto('/users')
    // Should be redirected to login page again
    await page.waitForURL('**/Account/Login**', { timeout: 15000 })
    expect(page.url()).toContain('/Account/Login')
  })

  test('api bearer', async ({ page }) => {
    const bearerHeaders: string[] = []

    // Intercept requests to /api/users and capture Authorization header
    page.on('request', (request) => {
      if (request.url().includes('/api/users')) {
        const authHeader = request.headers()['authorization']
        if (authHeader) {
          bearerHeaders.push(authHeader)
        }
      }
    })

    // Login
    await loginAsAdmin(page)

    // Manually trigger an API call via page.evaluate to verify Bearer header
    await page.evaluate(async () => {
      await fetch('http://localhost:5009/api/users', {
        headers: {
          Authorization: `Bearer ${(window as any).__getAccessToken?.() ?? ''}`,
        },
      })
    })

    // Alternatively, use the SPA's client by triggering navigation to /users
    // and checking any captured request headers
    // Navigate to /users which may trigger a fetch
    await page.reload()
    await page.waitForTimeout(1000)

    // Use page.evaluate to call the API via the SPA's own client and intercept at network level
    const authHeaderFound = await page.evaluate(async () => {
      const accessToken = window.sessionStorage
        ? (() => {
            for (let i = 0; i < window.sessionStorage.length; i++) {
              const key = window.sessionStorage.key(i)
              if (key && key.startsWith('oidc.user:')) {
                try {
                  const userData = JSON.parse(window.sessionStorage.getItem(key) ?? '{}')
                  return userData.access_token as string | undefined
                } catch {
                  return undefined
                }
              }
            }
            return undefined
          })()
        : undefined

      if (!accessToken) return false

      const response = await fetch('http://localhost:5009/api/users', {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
      return response.status === 200
    })

    expect(authHeaderFound).toBe(true)
  })
})
