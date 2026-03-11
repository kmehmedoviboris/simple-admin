import { defineConfig } from '@hey-api/openapi-ts'

export default defineConfig({
  input: 'http://localhost:5009/openapi/v1.json',
  output: {
    path: 'src/client',
  },
  client: '@hey-api/client-fetch',
})
