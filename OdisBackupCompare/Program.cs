using CommandLine;
using CommandLine.Text;
using Fededim.OdisBackupCompare.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NReco.Logging.File;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
            {
                services.AddTransient<OdisDataComparer>(sp => new OdisDataComparer(o, sp.GetRequiredService<ILogger<OdisDataComparer>>()));
            })
            .ConfigureLogging((context,loggerBuilder) =>
            {
                loggerBuilder
                .AddDebug()
                .AddConsole()
                .AddFile("app.log", append: true);
            }).Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            ComparisonResults comparisonResult = null;
            if (!String.IsNullOrEmpty(o.InputJson))
            {
                comparisonResult = JsonConvert.DeserializeObject<ComparisonResults>(File.ReadAllText(o.InputJson), Options.JsonSerializerOptions);

                comparisonResult.Options.OutputFormats = o.OutputFormats;
                comparisonResult.Options.Output = o.Output;
                comparisonResult.Options.ComparisonOptions = o.ComparisonOptions;
                comparisonResult.Options.EcuIds = comparisonResult.Options.EcuIds?.Count()>0 ? comparisonResult.Options.EcuIds.Intersect(o.EcuIds) : o.EcuIds;
                comparisonResult.Options.BypassFields = comparisonResult.Options.BypassFields.Union(o.BypassFields);

                logger.LogInformation($"Successfully read file {o.InputJson}");
            }
            else
            {
                var inputFiles = o.Inputs.ToArray();

                var firstOdisData = OdisData.ParseFromFile(inputFiles[0]);
                var secondOdisData = OdisData.ParseFromFile(inputFiles[1]);

                var firstEcus = firstOdisData.GetEcus();
                var secondEcus = secondOdisData.GetEcus();

                var odisDataComparer = host.Services.GetRequiredService<OdisDataComparer>();

                comparisonResult = odisDataComparer.CompareEcus(firstEcus, secondEcus);

                logger.LogInformation($"Successfully compared the ODIS XML files {inputFiles[0]} and {inputFiles[1]}");
            }

            var outFolder = o.Output ?? ".";
            var outFilename = $"OdisBackupCompare_{DateTime.Now.ToString("yyyy-MM-ddTHH_mm_ss_ffff")}";

            String outJsonFilename = null;
            String outPdfFilename = null;
            if (Directory.Exists(outFolder)) {
                outJsonFilename = Path.Combine(outFolder, $"{outFilename}.json");
                outPdfFilename = Path.Combine(outFolder, $"{outFilename}.pdf");
            }
            else
            {
                if (!String.IsNullOrEmpty(Path.GetDirectoryName(outFolder)))
                    Directory.CreateDirectory(Path.GetDirectoryName(outFolder));
                outJsonFilename = Path.ChangeExtension(outFolder,".json");
                outPdfFilename = Path.ChangeExtension(outFolder, ".pdf");
            }

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.JSON) && String.IsNullOrEmpty(o.InputJson))
            {

                File.WriteAllText(outJsonFilename, JsonConvert.SerializeObject(comparisonResult, Options.JsonSerializerOptions));
                logger.LogInformation($"Successfully created output JSON file {outJsonFilename}");
            }

            if (o.CheckEnumerableOption(o.OutputFormats, OutputFileFormatEnum.PDF))
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var pdfGeneratorLogger = host.Services.GetRequiredService<ILogger<PdfGenerator>>();

                if (o.SplitByEcu)
                {
                    foreach (var splitComparisonResult in comparisonResult.SplitByEcu())
                    {
                        var ecuId = splitComparisonResult.EcusComparisonResult.First().EcuId;
                        var outSplitPdfFilename = $"{Path.GetFileNameWithoutExtension(outPdfFilename)}_{ecuId}{Path.GetExtension(outPdfFilename)}";

                        var pdfGenerator = new PdfGenerator(splitComparisonResult, pdfGeneratorLogger);
                        pdfGenerator.GeneratePdf(outSplitPdfFilename);

                        logger.LogInformation($"Successfully created output PDF file {outSplitPdfFilename} for ECU {ecuId}");
                    }
                }
                else
                {
                    var pdfGenerator = new PdfGenerator(comparisonResult, pdfGeneratorLogger);
                    pdfGenerator.GeneratePdf(outPdfFilename);

                    logger.LogInformation($"Successfully created output PDF file {outPdfFilename}");
                }
            }

            return 1;
        }
    }
}