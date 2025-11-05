using CommandLine;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Timers;

namespace OdisBackupCompare
{
    public class Options
    {
        [Option('i', "inputs", Required = true, HelpText = "Specifies the two input files to process separated by space", Min = 2, Max = 2)]
        public IEnumerable<string> Inputs { get; set; }

        [Option('e', "ecus", Required = false, HelpText = "Specify the ecu ids which must be compared")]
        public IEnumerable<String> EcuIds { get; set; }

        [Option('o', "outputformat", Required = false, HelpText = "Specify the file formats to genenerate as output containing the result of the comparison", Default = new OutputFileFormatEnum[] { OutputFileFormatEnum.JSON, OutputFileFormatEnum.PDF })]
        public IEnumerable<OutputFileFormatEnum> OutputFormats { get; set; }

        [Option('f', "outputfolder", Required = false, HelpText = "Specify the output folder where all the output files will be generated")]
        public String OutputFolder { get; set; }

        [Option('c', "comparisonoptions", Required = false, HelpText = "Specify what to compare")]
        public IEnumerable<ComparisonOptionsEnum> ComparisonOptions { get; set; }

        [Option('b', "bypass", Required = false, HelpText = "Specifies one or more field types to be bypassed by the comparison separated by space", Default = new FieldPropertyEnum[] { FieldPropertyEnum.DisplayName, FieldPropertyEnum.TiValue })]
        public IEnumerable<FieldPropertyEnum> BypassFields { get; set; }

        public static JsonSerializerOptions JsonSerializerOptions { get; set; }


        public bool CheckEnumerableOption<T>(IEnumerable<T> options, T value)
        {
            return (options == null || !options.Any() || options.Contains(value));
        }

        static Options()
        {
            JsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            };
            JsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter());
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>
                                                  (o => CompareOdisXMLFiles(o));

            return 0;
        }


        private static void CompareOdisXMLFiles(Options o)
        {
            var inputFiles = o.Inputs.ToArray();

            var firstOdisData = OdisData.ParseFromFile(inputFiles[0]);
            var secondOdisData = OdisData.ParseFromFile(inputFiles[1]);

            var firstEcus = firstOdisData.GetEcus();
            var secondEcus = secondOdisData.GetEcus();

            var odisDataComparer = new OdisDataComparer(o);

            var comparisonResult = odisDataComparer.CompareEcus(firstEcus, secondEcus);

            var outFolder = o.OutputFolder ?? ".";
            var outFilename = $"OdisBackupCompare_{comparisonResult.Timestamp.ToString("yyyy-MM-ddTHH_mm_ss_ffff")}";

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.JSON))
            {
                File.WriteAllText(Path.Combine(outFolder, $"{outFilename}.json"), JsonSerializer.Serialize(comparisonResult, Options.JsonSerializerOptions));
            }

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.PDF))
            {
                QuestPDF.Settings.License = LicenseType.Community;
                var pdfGenerator = new PdfGenerator(comparisonResult);
                pdfGenerator.GeneratePdf(Path.Combine(outFolder, $"{outFilename}.pdf"));
            }
        }
    }
}