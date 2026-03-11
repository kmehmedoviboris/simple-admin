import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:5173',
    headless: true,
  },
  projects: [{ name: 'chromium', use: { browserName: 'chromium' } }],
  // Both backend (5009) and frontend (5173) must be running before tests
  webServer: [
    {
      command: 'cd ../SimpleAdmin.Api && dotnet run',
      url: 'http://localhost:5009/.well-known/openid-configuration',
      reuseExistingServer: true,
      timeout: 60000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
      timeout: 30000,
    },
  ],
})
