# CSV to JSON Converter Tool

This tool is a **one-time bootstrap utility** for developers. Its purpose is to generate the initial `countries.json` seed file from a source CSV, complete with new, unique GUIDs for each country.

**IMPORTANT:** The `Country Identifier` column in the source CSV will be **ignored**. The tool's primary purpose is to generate a new, stable GUID for each record.

## How to Use

### 1. Prepare the Source CSV

Create a file named `countries.csv` in this same directory (`tools/CsvToJsonConverter/`). The file **must** contain a header row with the following 14 columns in this exact order:

`Country Code,Country Identifier,Country Long Name,Country Short Name,Created By,Created Date,Devolved Authority,Effective End Date,Effective Start Date,EU Trade Member,Is Active,Last Modified By,Last Modified Date,Sort Order`

**Notes on Data Formatting:**
- **Dates:** Should be in a recognizable format (e.g., `yyyy-MM-dd` or `dd/MM/yyyy`). Empty values are acceptable for nullable dates.
- **Booleans:** Use `true` or `false` (case-insensitive).
- **Country Identifier:** This column's value will be ignored and replaced with a new GUID. You can leave it blank.

**Example `countries.csv`:**
```csv
Country Code,Country Identifier,Country Long Name,Country Short Name,Created By,Created Date,Devolved Authority,Effective End Date,Effective Start Date,EU Trade Member,Is Active,Last Modified By,Last Modified Date,Sort Order
GB,,United Kingdom of Great Britain and Northern Ireland,United Kingdom,InitialLoad,2023-01-01,false,,1900-01-01,false,true,,,10
US,,United States of America,United States,InitialLoad,2023-01-01,false,,1900-01-01,false,true,,,20
AF,,Afghanistan,Afghanistan,InitialLoad,2023-01-01,false,,1900-01-01,false,true,,,999