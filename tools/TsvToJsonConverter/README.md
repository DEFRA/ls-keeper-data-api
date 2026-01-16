# Generic Data Converter Tool

This tool is a **developer utility** for bootstrapping reference data. It reads a source TSV file, converts it to a structured JSON format with new GUIDs, and prepares it to be included in the application build.

**IMPORTANT:** This is a bootstrap tool. After the initial JSON file is generated and committed, it should be manually maintained. Only run this tool if you intend to completely regenerate a reference data file from a new source TSV.

## How to Use

### 1. Save Source XLSX as TSV

Select Save as and choose - Text (Tab Delimited)(*.txt) from the dropdown. Then change the file extension from .txt to .tsv

### 2. Place Source TSV(s)

Place your source TSV file(s) in this same directory ('tools/TsvToJsonConverter/'). The tool looks for specific filenames:
- For countries: 'countries.tsv'
- For species: 'species.tsv'

### 3. Run the Tool

Open a terminal in this directory ('tools/TsvToJsonConverter/') and run the 'dotnet run' command, specifying which data type you want to convert.

**To convert countries:**

dotnet run countries

**To convert species:**

dotnet run species

**To convert roles:**

dotnet run roles

**To convert premisestypes:**

dotnet run premisestypes

**To convert premisesactivitytypes:**

dotnet run premisesactivitytypes

**To convert siteidentifiertypes:**

dotnet run siteidentifiertypes

**To convert productionusages:**

dotnet run productionusages

**To convert facilitybusinessactivitymaps:**

dotnet run facilitybusinessactivitymaps