using CommandLine;
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

        [Option('o', "outputfolder", Required = false, HelpText = "Specify the output folder where all the output files will be generated")]
        public String OutputFolder { get; set; }

        [Option('b', "bypass", Required = false, HelpText = "Specifies one or more fields to be bypassed by the comparison separated by space", Default = new FieldPropertyEnum[] { FieldPropertyEnum.DisplayName })]
        public IEnumerable<FieldPropertyEnum> BypassFields { get; set; }
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

            var odisDataComparer = new OdisDataComparer(new OdisDataComparerSettings { FieldToBypassOnComparison = o.BypassFields.ToList() });

            var compareResult = odisDataComparer.CompareEcus(firstEcus, secondEcus);

            var outFile = Path.Combine(o.OutputFolder ?? ".", "OdisBackupCompare.json");

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
            };
            jsonSerializerOptions.Converters.Add(new JsonStringEnumMemberConverter());

            File.WriteAllText(outFile, JsonSerializer.Serialize(compareResult, jsonSerializerOptions));
        }
    }
}