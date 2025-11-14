using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Fededim.OdisBackupCompare.Data
{
    public class AppSettings
    {
        public ColorOptions ColorOptions { get; protected set; }

        public AppSettings()
        {
            ColorOptions = new ColorOptions();
        }
    }


    public class ColorOptions : Dictionary<String, String>
    {
        public static Dictionary<String, String> DefaultColors = new Dictionary<String, String>
        {
            { "DefaultTextColor", "#FF000000" },
            { "HeaderDifferenceTypeColor", "#FF006AFF" },
            { "HeaderDifferenceNumberColor", "#FFE91E63" },
            { "HeaderECUColor", "#FF673AB7" },
            { "HeaderTypeColor", "#FF35A03C" },
            { "DifferenceLeftCharacterColor", "#FFD32F2F" },
            { "DifferenceRightCharacterColor", "#FF388E3C" },
            { "DifferenceAdditionalColor", "#FF1976D2" },
            { "PageColor", "#FFFFFFFF" },
            { "TableHeaderColor", "#FFEEEEEE" },
            { "TableCellColor", "#FFFFFFFF" },
            { "TableBorderColor", "#FFBDBDBD" },
            { "TableDifferenceHeaderColor", "#FFEEEEEE" },
            { "TableDifferenceCellColor", "#FFFFFFFF" },
            { "TableDifferenceBorderColor", "#FFBDBDBD" },
        };

        public String GetColor(string key)
        {
            TryGetValue(key, out var value);

            if (!String.IsNullOrWhiteSpace(value))
                return value;

            return DefaultColors[key];
        }
    }


    public enum OutputFileFormatEnum { JSON, PDF };

    public enum ComparisonOptionsEnum { Differences, DataMissingInFirstFile, DataMissingInSecondFile };
    public enum DifferenceStatisticsEnum { DifferentValues, SettingsMissingInFirstFile, SettingsMissingInSecondFile, FieldsMissingInFirstFile, FieldsMissingInSecondFile };

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

        public AppSettings AppSettings { get; set; }

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




    public class SummaryComparisonResults
    {
        public String EcuId { get; set; }
        public String Type { get; set; }
        public DifferenceStatisticsEnum? DifferenceType { get; set; }
        public int Count { get; set; }
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



        public String GetStatisticsSummary()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Comparison summary\n");

            int[] numGroupPrints = new int[] { 0, 0, 0 };

            var statistics = GetStatistics();
            SummaryComparisonResults oldStat = null;

            for (int i = 0; i < statistics.Count; i++)
            {
                var stat = statistics[i];

                if (oldStat != null && ((String.IsNullOrEmpty(stat.EcuId)^ String.IsNullOrEmpty(oldStat.EcuId)) || ((String.IsNullOrEmpty(stat.Type) ^ String.IsNullOrEmpty(oldStat.Type)) && stat.EcuId != oldStat.EcuId) || (stat.DifferenceType.HasValue ^ oldStat.DifferenceType.HasValue)))
                    sb.AppendLine("");

                if (!String.IsNullOrEmpty(stat.EcuId) && !String.IsNullOrEmpty(stat.Type))
                    sb.AppendLine($"Ecu {stat.EcuId} - {stat.Type}: {stat.Count} differences");
                else
                {
                    if (!String.IsNullOrEmpty(stat.EcuId))
                        sb.AppendLine($"Ecu {stat.EcuId}: {stat.Count} differences");
                    else if (!String.IsNullOrEmpty(stat.Type))
                        sb.AppendLine($"{stat.Type}: {stat.Count} differences");
                    if (stat.DifferenceType.HasValue)
                        sb.AppendLine($"{stat.DifferenceType}: {stat.Count} differences");
                }

                oldStat = stat;
            }

            return sb.ToString();
        }




        public List<SummaryComparisonResults> GetStatistics()
        {
            var results = new List<SummaryComparisonResults>();

            foreach (var ecr in EcusComparisonResult)
            {
                if (!Options.CheckEcuIds(ecr.EcuId))
                    continue;

                // MASTER ECU DATA MISSING
                if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                    results.AddRange(ecr.MasterEcuDataMissingInFirst.Select(mf => new SummaryComparisonResults { EcuId = ecr.EcuId, DifferenceType = DifferenceStatisticsEnum.SettingsMissingInFirstFile, Type = mf.Key, Count = mf.Key.Length }).ToList());

                if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                    results.AddRange(ecr.MasterEcuDataMissingInSecond.Select(ms => new SummaryComparisonResults { EcuId = ecr.EcuId, DifferenceType = DifferenceStatisticsEnum.SettingsMissingInSecondFile, Type = ms.Key, Count = ms.Key.Length }).ToList());

                // MASTER ECU DATA DIFFERENCES
                if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.Differences))
                    foreach (var ec in ecr.MasterEcuDataComparisonResult)
                    {
                        if (ec.FieldsMissingInFirst?.Count > 0 && Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                            results.Add(new SummaryComparisonResults { EcuId = ecr.EcuId, DifferenceType = DifferenceStatisticsEnum.FieldsMissingInFirstFile, Type = MeaningfulText.Map(ec.First.Type), Count = ec.FieldsMissingInFirst.Count });

                        if (ec.FieldsMissingInSecond?.Count > 0 && Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                            results.Add(new SummaryComparisonResults { EcuId = ecr.EcuId, DifferenceType = DifferenceStatisticsEnum.FieldsMissingInSecondFile, Type = MeaningfulText.Map(ec.First.Type), Count = ec.FieldsMissingInSecond.Count });

                        if (ec.Differences?.Count > 0)
                            results.Add(new SummaryComparisonResults { EcuId = ecr.EcuId, DifferenceType = DifferenceStatisticsEnum.DifferentValues, Type = MeaningfulText.Map(ec.First.Type), Count = ec.Differences.Count(df => !Options.CheckEnumerableOption(Options.BypassFields, df.FieldProperty)) });
                    }

                // SUBSSYSTEM DATA MISSING IN FIRST
                if (ecr.SubsystemEcuDataMissingInFirst != null && Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                    foreach (var kvp in ecr.SubsystemEcuDataMissingInFirst)
                    {
                        var EcuIdType = EcuComparisonResult.ExtractSubsystemEcuAndTypeFromKey(kvp.Key);
                        results.Add(new SummaryComparisonResults { EcuId = $"{ecr.EcuId}_SUB_{EcuIdType.EcuId}", DifferenceType = DifferenceStatisticsEnum.SettingsMissingInFirstFile, Type = EcuIdType.Type, Count = kvp.Value.Values.Count });
                    }

                // SUBSSYSTEM DATA MISSING IN SECOND
                if (ecr.SubsystemEcuDataMissingInSecond != null && Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                    foreach (var kvp in ecr.SubsystemEcuDataMissingInSecond)
                    {
                        var EcuIdType = EcuComparisonResult.ExtractSubsystemEcuAndTypeFromKey(kvp.Key);
                        results.Add(new SummaryComparisonResults { EcuId = $"{ecr.EcuId}_SUB_{EcuIdType.EcuId}", DifferenceType = DifferenceStatisticsEnum.SettingsMissingInSecondFile, Type = EcuIdType.Type, Count = kvp.Value.Values.Count });
                    }

                // SUBSSYSTEM DATA DIFFERENCES
                if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.Differences))
                    foreach (var sc in ecr.SubsystemEcuDataComparisonResult)
                    {
                        var EcuIdType = EcuComparisonResult.ExtractSubsystemEcuAndTypeFromKey(sc.Path[2]);

                        if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                            results.Add(new SummaryComparisonResults { EcuId = $"{ecr.EcuId}_SUB_{EcuIdType.EcuId}", DifferenceType = DifferenceStatisticsEnum.FieldsMissingInFirstFile, Type = EcuIdType.Type, Count = sc.FieldsMissingInFirst.Count });

                        if (Options.CheckEnumerableOption(Options.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                            results.Add(new SummaryComparisonResults { EcuId = $"{ecr.EcuId}_SUB_{EcuIdType.EcuId}", DifferenceType = DifferenceStatisticsEnum.FieldsMissingInSecondFile, Type = EcuIdType.Type, Count = sc.FieldsMissingInSecond.Count });

                        results.Add(new SummaryComparisonResults { EcuId = $"{ecr.EcuId}_SUB_{EcuIdType.EcuId}", DifferenceType = DifferenceStatisticsEnum.DifferentValues, Type = EcuIdType.Type, Count = sc.Differences.Count(df => !Options.CheckEnumerableOption(Options.BypassFields, df.FieldProperty)) });
                    }
            }

            // ROLL UP NUMBERS
            var rollUps = new List<SummaryComparisonResults>();
            rollUps.AddRange(results.GroupBy(r => r.Type).Select(g => new SummaryComparisonResults { EcuId = null, DifferenceType = null, Type = g.Key, Count = g.Sum(r => r.Count) }).OrderBy(r=> r.Type));
            rollUps.AddRange(results.GroupBy(r => r.DifferenceType).Select(g => new SummaryComparisonResults { EcuId = null, Type = null, DifferenceType = g.Key, Count = g.Sum(r => r.Count) }).OrderBy(r => r.DifferenceType));

            var ecusRollups = new List<SummaryComparisonResults>();
            ecusRollups.AddRange(results.GroupBy(r => r.EcuId).Select(g => new SummaryComparisonResults { EcuId = g.Key, DifferenceType = null, Type = null, Count = g.Sum(r => r.Count) }));
            ecusRollups.AddRange(results.GroupBy(r => (r.EcuId, r.Type)).Select(g => new SummaryComparisonResults { EcuId = g.Key.EcuId, Type = g.Key.Type, DifferenceType = null, Count = g.Sum(r => r.Count) }));
            ecusRollups = ecusRollups.OrderBy(ec => ec.EcuId).ThenBy(ec => ec.Type).ToList();

            rollUps.AddRange(ecusRollups);

            return rollUps;
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

        public static Regex SubsystemDataKey = new Regex($"(?<subsystem>.+)_(?<type>{String.Join('|', MeaningfulText.RemapData.Values.ToList())})");


        public static (String EcuId, String Type) ExtractSubsystemEcuAndTypeFromKey(string key)
        {
            var match = SubsystemDataKey.Match(key);
            if (match.Success)
                return (match.Groups["subsystem"].Value, match.Groups["type"].Value);

            return (null, null);
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
