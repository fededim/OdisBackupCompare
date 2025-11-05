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

**OdisBackupCompare** consists of a single executable and it is available for **Windows, Linux, Mac X64/ARM64** (it has been developed with Microsoft .NET). Download the latest archive [from realeases](https://github.com/fededim/OdisBackupCompare/releases) and decompress it whenever you want. You must have installed .NET 8.0 to execute it from command prompt / shell.

Two samples ODIS XML backup files (one is from my car) are provided in the archive folder **SampleXML**.


## Command line help

OdisBackupCompare 1.0.0+efe77bd492f87ac32158c6e1312809fdbd8e1b31
2025 Federico Di Marco &lt;fededim@gmail.com>

  -i, --inputs               Required. Specifies the two Odis XML files to
                             process separated by space<br/>
  -e, --ecus                 Specify the ecu ids which must be compared<br/>
  -m, --outputformat         (Default: JSON PDF) Specify the file formats to
                             genenerate as output containing the result of the
                             comparison<br/>
  -f, --outputfolder         Specify the output folder where all the output
                             files will be generated<br/>
  -o, --comparisonoptions    (Default: Differences DataMissingInFirstFile
                             DataMissingInSecondFile) Specify what to compare<br/>
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

**Compare two Odis XML files and generate only PDF file**<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -m PDF

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
(this is an expert function whick skips the checks on some fields of the XML file, probably you will never have to use it, I used it to during development to get out the most significant differences. In fact by default the field DisplayName is skipped because Odis is localized and if you happen to have two Odis XML files generated with different locale setting [e.g. English-Russian] the program could output a lot differences)<br/><br/>
OdisBackupCompare -i &lt;path to XML file1&gt; &lt;path to XML file2&gt; -b DisplayValue

# Sample PDF generated using the backup of my car (all control unit 280 pages!)
[Download here from GitHub](https://raw.githubusercontent.com/fededim/OdisBackupCompare/main/OdisBackupCompare/SampleXML/OdisBackupCompare_output_sample.pdf)

The full unfiltered PDF is composed of these sections:
- ECUs missing in first file
- ECUs missing in second file
- For every ECUs
    - Informations/Codings/Adaptations not found in first file
    - Informations/Codings/Adaptations differences between common entries
    - Informations/Codings/Adaptations not found in second file


# Notice of Non-Affiliation and Disclaimer
I'm not affiliated, associated, authorized, endorsed by, or in any way connected with the VOLKSWAGEN AG, or any of its subsidiaries or its affiliates. The official VOLKSWAGEN AG software or tools can be found at https://www.vw.com/.

The names VOLKWAGEN, VW and ODIS as well as related names, marks, emblems and images are registered trademarks of their respective owners.
