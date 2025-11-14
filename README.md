# OdisBackupCompare (&copy; 2025 Federico Di Marco &lt;fededim@gmail.com&gt; released under MIT license)
**OdisBackupCompare** is a tool for extracting the differences between two Volkswagen Odis XML backup files and exporting them in various formats (JSON/PDF).

It can be used to check the differences of information, codings and adaptations between two cars in order to simplify and speed up the troubleshooting of issues after a retrofit. I developed and tested it thoroughly on my Golf MK7 Facelift, it supports all electronic control units, even those connected to the secondary buses; high likely it should work also with any control unit from a VW group car.

# How to use it

## PREREQUISITE
- **Perform a full backup through **Volkswagen Odis** of the two cars which must be compared and obtain two XML files to be compared** (ODIS creates also a HTML file for displaying all the data inside a webpage, but it is not needed). Steps:
    1. Vehicle functions -> 046 Vehicle Special Functions -> 046.01 Coding / Adaptations
    1. Check Read KWP control module
    1. Check Adaptations / Codes under "Data from all control modules"
    1. Click button "Read Data"
- If you do not have already, [install .NET 8.0 framework runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

**OdisBackupCompare** consists of a single executable and it is available for **Windows, Linux, Mac X64/ARM64** (it has been developed with Microsoft .NET). Download the latest archive [from releases](https://github.com/fededim/OdisBackupCompare/releases) and decompress it whenever you want. You must have installed .NET 8.0 to execute it from command prompt / shell.

Two samples ODIS XML backup files (one is from my car) are provided in the archive folder **SampleXML**.


## Command line help

OdisBackupCompare 1.0.0+efe77bd492f87ac32158c6e1312809fdbd8e1b31
2025 Federico Di Marco &lt;fededim@gmail.com>

  -i, --inputs               Required. Specifies the two Odis XML files to
                             process separated by space<br/>
  -e, --ecus                 Specifies the ecu ids which must be compared<br/>
  -m, --outputformat         (Default: JSON PDF) Specifies the file formats to
                             genenerate as output containing the result of the
                             comparison<br/>
  -f, --outputfolder         Specifies the output folder or filename where all the output
                             data will be stored<br/>
  -s, --splitbyecu           Specifies to split the output file in multiple files, one for
                             each ecu. Ignored for JSON output.<br/>
  -o, --comparisonoptions    (Default: Differences DataMissingInFirstFile
                             DataMissingInSecondFile) Specifies what to compare<br/>
  -b, --bypass               (Default: DisplayName TiValue) Specifies one or
                             more field types to be bypassed by the comparison
                             separated by space
  -j, --inputjson            Required (exclusive with -i). Specifies the processed JSON file to
                             reload<br/><br/>
  --help                     Display this help screen.<br/>
  --version                  Display version information.
<br/>
<br/><br/>

## Command line examples

**Compare two Odis XML files and generate all output files (JSON/PDF).**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt;

**Compare two Odis XML files and generate all output files (JSON/PDF) in folder c:\temp.**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -f c:\temp

**Compare two Odis XML files and generate all output files (JSON/PDF) in files c:\temp\test.\* **<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -f c:\temp

**Compare two Odis XML files and generate only PDF file**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -m PDF

**Compare two Odis XML files and generate only PDF files, one file for each ECUs**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -m PDF -s

**Compare two Odis XML files and generate all output files (JSON/PDF) only for ECUs with IDs 19 (0019 -> Gateway), 4B (004B --> Multi function module).**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -e 19 4b

**Compare two Odis XML files and generate all output files (JSON/PDF) only for the differences between the COMMON codings/adaptations, do not list any unique coding/adaptation present in only one of the control units**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -o Differences

**Compare two Odis XML files and generate all output files (JSON/PDF) showing only the codings/adaptations of the second file not found in first file** (this can happen if the control units have different firmware or even different hardware part numbers)<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -o DataMissingInFirstFile

**Compare two Odis XML files and generate all output files (JSON/PDF) showing only the codings/adaptations of the first file not found in second file**<br/> (this can happen if the control units have different firmware or even different hardware part numbers)<br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -o DataMissingInSecondFile

**Reload an already processed comparison file (JSON) and regenerate the PDF with only the ecu 44 (0044 -> Power Steering)**<br/>
OdisBackupCompare -j &lt;path to JSON file&gt; -e 44

**Compare two Odis XML files and generate all output files (JSON/PDF) bypassing the comparison on DisplayValue fields**<br/>
(this is an expert function whick skips the checks on some fields of the XML file, probably you will never have to use it, I used it to during development to get out the most significant differences. In fact, by default the field DisplayName is skipped because Odis is localized and if you happen to have two Odis XML files generated with different locale setting [e.g. English-Russian] the program could output a lot differences)<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -b DisplayValue

# Sample PDF generated using the backup of my car (all control units compared in 280 pages!)
[Download here from GitHub](https://raw.githubusercontent.com/fededim/OdisBackupCompare/main/OdisBackupCompare/SampleXML/OdisBackupCompare_output_sample.pdf)

The full unfiltered PDF is composed of these sections:
- ECUs missing in first file
- ECUs missing in second file
- For every ECUs
    - Identification/Codings/Adaptations not found in first file
    - Identification/Codings/Adaptations differences between common entries
    - Identification/Codings/Adaptations not found in second file

# Notice of Non-Affiliation and Disclaimer
I'm not affiliated, associated, authorized, endorsed by, or in any way connected with the VOLKSWAGEN AG, or any of its subsidiaries or its affiliates. The official VOLKSWAGEN AG software or tools can be found at https://www.vw.com/.

The names VOLKWAGEN, VW and ODIS as well as related names, marks, emblems and images are registered trademarks of their respective owners.

# Sample screenshots

Numerical values are compared figure by figure and differences are highlighted in:
- red in the first file (highlighting the possible error)
- green in the second file (highlighting the possible good value)
- blue in the second file for any figure not found in the first one (highlighting either a bigger value or a difference in values length due to the different firmware/hardware part numbers)

## Missing ECUs

<img width="2333" height="1652" alt="image" src="https://github.com/user-attachments/assets/0c96f752-5089-4882-a370-7c760bc0910e" />

## Missing ECUS from subsystems

<img width="2328" height="1628" alt="image" src="https://github.com/user-attachments/assets/5bf2ac92-9f71-44fb-b644-cb4c6e2f3aad" />

## Identification differences

<img width="2306" height="1613" alt="image" src="https://github.com/user-attachments/assets/782dfc2c-afdd-4732-b116-94cb9373b625" />

## Coding differences

<img width="2312" height="1631" alt="image" src="https://github.com/user-attachments/assets/4d955415-b66b-42f6-b766-48f40d545310" />

## Adaptation differences

<img width="2312" height="1616" alt="image" src="https://github.com/user-attachments/assets/5c7a1e25-8e42-4492-9f12-cacfe3b7f5cd" />

## Missing field between two control units (comparison of 0001 Engine Electronics between a diesel and a petrol Golf)

<img width="2312" height="1621" alt="image" src="https://github.com/user-attachments/assets/4dfe6833-b410-4653-b38b-b27e4eb285de" />

# Version history

v1.0.0
- Initial version

v1.0.1
- Bugfix on layout of missing fields
- Fixed typo in help text

v1.0.2
- Bugfix: skipped the generation of ECUs MISSING IN FIRST/SECOND FILE section when they are empty
- Improvement: added option -s or --splitbyecu to generate a single PDF file for every ECUs
- Bugfix: fixed crash when specifying an output file in the current directory

v1.0.3
- Bugfix/improvement: improved and fixed page breaks
- Improvement: added output of missing ecus file in single file per ecu mode
- Improvement: added colors to key elements inside the table headers
- Bugfix: fixed color of numerical values differences

v1.0.4
- Improvement: added optional standard configuration file named appsetting.json
- Improvement: added customization of colors inside an optional appsetting.json file for theming PDF output files

v1.0.5
- Bugfix: removed duplicated row in a table header
- Improvement: added general comparison statistics in PDF output
- Improvement: changed the color of the type field (adaptation, coding, identification)
