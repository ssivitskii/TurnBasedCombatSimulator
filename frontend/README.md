# Runebound Arena UI

Angular 22 client for the Turn-Based Combat Simulator. It edits battle configuration and replays events returned by the .NET API; it contains no combat rules.

From the repository root, start the API on port `8080`. Then run:

```bash
cd frontend
npm install
npm start
```

The development proxy forwards `/api` and `/health` to the API. Use `npm test`, `npm run build`, and `npm run format:check` for verification.
