# Generic Data Converter Tool

This tool is a **developer utility** for bootstrapping reference data. It reads a source CSV file, converts it to a structured JSON format with new GUIDs, and prepares it to be included in the application build.

**IMPORTANT:** This is a bootstrap tool. After the initial JSON file is generated and committed, it should be manually maintained. Only run this tool if you intend to completely regenerate a reference data file from a new source CSV.

## How to Use

### 1. Place Source CSV(s)

Place your source CSV file(s) in this same directory ('tools/CsvToJsonConverter/'). The tool looks for specific filenames:
- For countries: 'countries.csv'
- For species: 'species.csv'

### 2. Run the Tool

Open a terminal in this directory ('tools/CsvToJsonConverter/') and run the 'dotnet run' command, specifying which data type you want to convert.

**To convert countries:**

dotnet run countries

**To convert species:**

dotnet run species

**To convert roles:**

dotnet run roles