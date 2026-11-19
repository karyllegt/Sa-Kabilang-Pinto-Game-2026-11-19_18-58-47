# Prototype verification

`verify_prototype.js` runs 20 behavior checks against the browser prototype with headless Chrome.

    npm i puppeteer-core
    node verify_prototype.js ../../reference/prototype/index.html "/path/to/chrome"

Last run (Nov 20, 2026): 20 of 20 passed, see `prototype_verification_report.json`. It is an oracle for Unity's behavior, not a replacement for Unity tests.
