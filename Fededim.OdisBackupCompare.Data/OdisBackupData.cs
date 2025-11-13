using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Fededim.OdisBackupCompare.Data
{
    public enum OutputFileFormatEnum { JSON, PDF };

    public enum ComparisonOptionsEnum { Differences, DataMissingInFirstFile, DataMissingInSecondFile };

    [Flags]
    public enum FieldParametersEnum { IsFreeText = 1, IsNumerical = 2 }

    public enum FieldPropertyEnum
    {
        [EnumMember(Value = "TI_NAME")]
        TiName,

        [EnumMember(Value = "TI_UNIT")]
        TiUnit,

        [EnumMember(Value = "DISPLAY_NAME")]
        DisplayName,

        [EnumMember(Value = "DISPLAY_VALUE")]
        DisplayValue,

        [EnumMember(Value = "DISPLAY_UNIT")]
        DisplayUnit,

        [EnumMember(Value = "BIN_VALUE")]
        BinValue,

        [EnumMember(Value = "HEX_VALUE")]
        HexValue,

        [EnumMember(Value = "TI_VALUE")]
        TiValue
    };


    public class Options
    {
        [Option('i', "inputs", Required = true, HelpText = "Specifies the two Odis XML files to process separated by space", Min = 2, Max = 2, SetName = "input_xml")]
        public IEnumerable<string> Inputs { get; set; }

        [Option('e', "ecus", Required = false, HelpText = "Specifies the ecu ids which must be compared")]
        public IEnumerable<String> EcuIds { get; set; }

        [Option('m', "outputformat", Required = false, HelpText = "Specifies the file formats to generate as output containing the result of the comparison", Default = new OutputFileFormatEnum[] { OutputFileFormatEnum.JSON, OutputFileFormatEnum.PDF })]
        public IEnumerable<OutputFileFormatEnum> OutputFormats { get; set; }

        [Option('f', "outputfolder", Required = false, HelpText = "Specifies the output folder or filename where all the output data will be stored")]
        public String Output { get; set; }

        [Option('s', "splitbyecu", Required = false, HelpText = "Specifies to split the output file in multiple files, one for each ecu. Ignored for JSON output.")]
        public Boolean SplitByEcu { get; set; }

        [Option('o', "comparisonoptions", Required = false, HelpText = "Specifies what to compare", Default = new ComparisonOptionsEnum[] { ComparisonOptionsEnum.Differences, ComparisonOptionsEnum.DataMissingInFirstFile, ComparisonOptionsEnum.DataMissingInSecondFile })]
        public IEnumerable<ComparisonOptionsEnum> ComparisonOptions { get; set; }

        [Option('b', "bypass", Required = false, HelpText = "Specifies one or more field types to be bypassed by the comparison separated by space", Default = new FieldPropertyEnum[] { FieldPropertyEnum.DisplayName, FieldPropertyEnum.TiValue })]
        public IEnumerable<FieldPropertyEnum> BypassFields { get; set; }

        [Option('j', "inputjson", Required = true, HelpText = "Specifies the processed JSON file to reload", SetName = "input_json")]
        public String InputJson { get; set; }

        //[Option('u', "invariantculture", Required = false, HelpText = "Specifies the use of invariant culture when comparing numeric fields to reduce the number of differences due to number culture-specific separators")]
        //public bool UseInvariantCulture { get; set; }

        public static JsonSerializerSettings JsonSerializerOptions { get; set; }


        public bool CheckEnumerableOption<T>(IEnumerable<T> options, T value)
        {
            return (options == null || !options.Any() || options.Contains(value));
        }

        public bool CheckEcuIds(string ecuId)
        {
            return (EcuIds == null || !EcuIds.Any() || EcuIds.Any(e => ecuId.Contains(e, StringComparison.InvariantCultureIgnoreCase)));
        }


        static Options()
        {
            JsonSerializerOptions = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            JsonSerializerOptions.Converters.Add(new StringEnumConverter());
        }
    }



    public class ComparisonResults
    {
        public DateTime Timestamp { get; set; }
        public Options Options { get; set; }

        public Dictionary<String, Ecu> EcusMissingInFirst { get; set; }
        public Dictionary<String, Ecu> EcusMissingInSecond { get; set; }

        public DictionaryList<EcuComparisonResult> EcusComparisonResult { get; set; } = new DictionaryList<EcuComparisonResult>((ecu, uniqueIndex) => ecu.EcuId);

        public ComparisonResults()
        {
            EcusMissingInFirst = new Dictionary<String, Ecu>();
            EcusMissingInSecond = new Dictionary<String, Ecu>();
        }


        public ComparisonResults(Options options, DateTime? timestamp = null) : this()
        {
            if (timestamp.HasValue)
                Timestamp = timestamp.Value;
            else
                Timestamp = DateTime.Now;

            Options = options;
        }


        public List<ComparisonResults> SplitByEcu()
        {
            var results = new List<ComparisonResults>() { new ComparisonResults(Options, Timestamp) { EcusMissingInFirst = EcusMissingInFirst, EcusMissingInSecond = EcusMissingInSecond } };

            foreach (var ecu in EcusComparisonResult)
            {
                var result = new ComparisonResults(Options, Timestamp);
                result.EcusComparisonResult.Add(ecu);
                results.Add(result);
            }

            return results;
        }
    }



    public class EcuComparisonResult
    {
        public String EcuId { get; set; }
        //public String[] EcuNames => new String[] { FirstEcu.ToString(), SecondEcu.ToString() }.Distinct().ToArray();

        public Ecu FirstEcu { get; set; }
        public Ecu SecondEcu { get; set; }

        // MASTER DATA
        public Dictionary<String, EcuData> MasterEcuDataMissingInFirst { get; set; }
        public Dictionary<String, EcuData> MasterEcuDataMissingInSecond { get; set; }


        public List<EcuDataComparisonResult> MasterEcuDataComparisonResult { get; set; }


        // SUBSYSTEM DATA
        public Dictionary<String, EcuData> SubsystemEcuDataMissingInFirst { get; set; }
        public Dictionary<String, EcuData> SubsystemEcuDataMissingInSecond { get; set; }

        public List<EcuDataComparisonResult> SubsystemEcuDataComparisonResult { get; set; }


        public EcuComparisonResult()
        {
            MasterEcuDataComparisonResult = new List<EcuDataComparisonResult>();
            MasterEcuDataMissingInFirst = new Dictionary<String, EcuData>();
            MasterEcuDataMissingInSecond = new Dictionary<String, EcuData>();

            SubsystemEcuDataComparisonResult = new List<EcuDataComparisonResult>();
            SubsystemEcuDataMissingInFirst = new Dictionary<String, EcuData>();
            SubsystemEcuDataMissingInSecond = new Dictionary<String, EcuData>();
        }

        [JsonIgnore]
        public bool IsEmpty => MasterEcuDataMissingInFirst.Count == 0 && MasterEcuDataMissingInSecond.Count == 0 && MasterEcuDataComparisonResult.Count == 0 &&
            SubsystemEcuDataMissingInFirst?.Count == 0 && SubsystemEcuDataMissingInSecond?.Count == 0 && SubsystemEcuDataComparisonResult?.Count == 0;
    }



    public class EcuDataComparisonResult
    {
        public List<String> Path { get; set; }

        public List<String> Descriptions { get; set; }

        public Dictionary<String, ValueItem> FieldsMissingInFirst { get; set; }
        public Dictionary<String, ValueItem> FieldsMissingInSecond { get; set; }

        public List<DifferenceMessage> Differences { get; set; }

        [JsonIgnore]
        public EcuData First { get; set; }
        [JsonIgnore]
        public EcuData Second { get; set; }


        public EcuDataComparisonResult()
        {
            Differences = new List<DifferenceMessage>();

            Path = new List<String>();
            FieldsMissingInFirst = new Dictionary<String, ValueItem>();
            FieldsMissingInSecond = new Dictionary<String, ValueItem>();
        }

        [JsonIgnore]
        public bool IsEmpty => FieldsMissingInFirst.Count == 0 && FieldsMissingInSecond.Count == 0 && Differences.Count == 0;
    }


    public class DifferenceMessage
    {
        public List<String> Path { get; set; }
        public List<String> FieldDescriptions { get; set; }
        public FieldPropertyEnum FieldProperty { get; set; }
        public FieldParametersEnum FieldParameters { get; set; }
        public String Message { get; set; }
        public String FirstValue { get; set; }
        public String SecondValue { get; set; }

        public DifferenceMessage()
        {
            Path = new List<String>();
        }


        public String GetPathDisplayString(char separator = '→')
        {
            // PATH:
            // if FieldDescription is populated remove the last element
            // if path[1] == subsystem skip first 3 elements else the first 2 elements (their data is already shown on the table header)
            var numberOfInitialElementsToSkip = (Path[1] == "subsystem") ? 3 : 2;
            var displayPath = Path.Skip(numberOfInitialElementsToSkip).Take(Path.Count - numberOfInitialElementsToSkip).ToArray();
            if (FieldDescriptions.Any())
                displayPath[displayPath.Length - 1] = FieldDescriptions.First();

            return String.Join(separator, displayPath);
        }
    }
}
