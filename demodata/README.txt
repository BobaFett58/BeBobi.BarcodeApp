Manual QA demo files for BarcodeApp import/edit/export flow.

Files:
- 01_valid_comma.csv: Standard comma-delimited with clean valid rows.
- 02_valid_semicolon.csv: Semicolon-delimited import check.
- 03_polish_alias_headers.csv: Header aliases (kodkreskowy, nazwa, ilosc).
- 04_no_header_fallback.csv: No header row; parser should use first 3 columns.
- 05_mixed_invalid_rows.csv: Invalid EANs, missing names, bad quantities.
- 06_tab_delimited.csv: Tab-delimited import check.

Quick manual flow:
1. Import one file.
2. Verify valid/invalid counters and validation messages.
3. Fix invalid rows in UI.
4. Export ZPL and inspect generated labels count.
5. Repeat with another file to test delimiter/header edge cases.
