using CommandLine;
using CommandLine.Text;
using Fededim.OdisBackupCompare.Data;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OdisBackupCompare
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);

            parserResult
                .WithParsed<Options>(options => CompareOdisXMLFiles(options))
                .WithNotParsed(errs => DisplayHelp(parserResult));
        }

        static int DisplayHelp<T>(ParserResult<T> result)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            });  //without the option e=>e
            Console.WriteLine(helpText);

            return 0;
        }


        private static int CompareOdisXMLFiles(Options o)
        {
            ComparisonResults comparisonResult = null;
            if (!String.IsNullOrEmpty(o.InputJson))
            {
                comparisonResult = JsonSerializer.Deserialize<ComparisonResults>(File.ReadAllText(o.InputJson), Options.JsonSerializerOptions);
            }
            else
            {
                var inputFiles = o.Inputs.ToArray();

                var firstOdisData = OdisData.ParseFromFile(inputFiles[0]);
                var secondOdisData = OdisData.ParseFromFile(inputFiles[1]);

                var firstEcus = firstOdisData.GetEcus();
                var secondEcus = secondOdisData.GetEcus();

                var odisDataComparer = new OdisDataComparer(o);

                comparisonResult = odisDataComparer.CompareEcus(firstEcus, secondEcus);
            }

            var outFolder = o.OutputFolder ?? ".";
            var outFilename = $"OdisBackupCompare_{comparisonResult.Timestamp.ToString("yyyy-MM-ddTHH_mm_ss_ffff")}";

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.JSON) && String.IsNullOrEmpty(o.InputJson))
            {
                File.WriteAllText(Path.Combine(outFolder, $"{outFilename}.json"), JsonSerializer.Serialize(comparisonResult, Options.JsonSerializerOptions));
            }

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.PDF))
            {
                QuestPDF.Settings.License = LicenseType.Community;
                var pdfGenerator = new PdfGenerator(comparisonResult);
                pdfGenerator.GeneratePdf(Path.Combine(outFolder, $"{outFilename}.pdf"));
            }

            return 1;
        }
    }
}