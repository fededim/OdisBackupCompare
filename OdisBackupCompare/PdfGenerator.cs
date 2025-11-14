using Fededim.OdisBackupCompare.Data;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OdisBackupCompare
{
    public class PdfGenerator : IDocument
    {
        protected ComparisonResults Results { get; }

        protected ILogger<PdfGenerator> Logger { get; }

        public PdfGenerator(ComparisonResults results, ILogger<PdfGenerator> logger)
        {
            Results = results;
            Logger = logger;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            var options = Results.Options;

            container.Page(page =>
                {
                    NewPage(page);

                    page.Content().Column(column =>
                    {
                        GenerateStatisticsPage(column, Results);

                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile) && Results.EcusMissingInFirst.Any())
                            AddMissingEcusNewPage(column, TextDescriptorFromFormattableString($"ECUs MISSING IN FIRST FILE ({Results.EcusMissingInFirst.Count:@HeaderDifferenceNumberColor})"), Results.EcusMissingInFirst);

                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile) && Results.EcusMissingInSecond.Any())
                            AddMissingEcusNewPage(column, TextDescriptorFromFormattableString($"ECUs MISSING IN SECOND FILE ({Results.EcusMissingInSecond.Count:@HeaderDifferenceNumberColor})"), Results.EcusMissingInSecond);

                        foreach (var ecuComparison in Results.EcusComparisonResult)
                        {
                            if (options.CheckEcuIds(ecuComparison.FirstEcu.EcuId))
                            {
                                AddEcuComparisonPage(column, new List<Action<TextDescriptor>> { TextDescriptorFromFormattableString($"ECU: {ecuComparison.FirstEcu.EcuId:@HeaderECUColor} ({ecuComparison.FirstEcu.EcuName:@HeaderECUColor})"), TextDescriptorFromFormattableString($"ECU: {ecuComparison.SecondEcu.EcuId:@HeaderECUColor} ({ecuComparison.SecondEcu.EcuName:@HeaderECUColor})") }, ecuComparison);
                            }
                        }
                    });
                });
        }


        protected void GenerateStatisticsPage(ColumnDescriptor column, ComparisonResults result)
        {
            column.Item().Padding(10).MinHeight(300).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.9f);
                    columns.RelativeColumn(0.1f);
                });

                table.Header(header =>
                {
                    header.Cell().Element(DifferenceHeaderCellStyle).AlignCenter().Text("Comparison summary").Bold();
                    header.Cell().Element(DifferenceHeaderCellStyle).AlignCenter().Text("Count").Bold();
                });

                int[] numGroupPrints = new int[] { 0, 0, 0 };

                var statistics = result.GetStatistics();
                SummaryComparisonResults oldStat = null;

                for (int i = 0; i < statistics.Count; i++)
                {
                    var stat = statistics[i];

                    if (oldStat != null && ((String.IsNullOrEmpty(stat.EcuId) ^ String.IsNullOrEmpty(oldStat.EcuId)) || ((String.IsNullOrEmpty(stat.Type) ^ String.IsNullOrEmpty(oldStat.Type)) && stat.EcuId != oldStat.EcuId) || (stat.DifferenceType.HasValue ^ oldStat.DifferenceType.HasValue)))
                        table.Cell().ColumnSpan(2).Element(DifferenceCellStyle).AlignLeft().ShowEntire().Text("");

                    if (!String.IsNullOrEmpty(stat.EcuId) && !String.IsNullOrEmpty(stat.Type))
                        table.Cell().Element(DifferenceCellStyle).AlignLeft().Text(TextDescriptorFromFormattableString($"{stat.EcuId:@HeaderECUColor} - {stat.Type:@HeaderTypeColor}"));
                    else
                    {
                        if (!String.IsNullOrEmpty(stat.EcuId))
                            table.Cell().Element(DifferenceCellStyle).AlignLeft().ShowEntire().Text(TextDescriptorFromFormattableString($"{stat.EcuId:@HeaderECUColor}"));
                        if (!String.IsNullOrEmpty(stat.Type))
                            table.Cell().Element(DifferenceCellStyle).AlignLeft().ShowEntire().Text(TextDescriptorFromFormattableString($"{stat.Type:@HeaderTypeColor}"));
                        if (stat.DifferenceType.HasValue)
                            table.Cell().Element(DifferenceCellStyle).AlignLeft().ShowEntire().Text(TextDescriptorFromFormattableString($"{stat.DifferenceType:@HeaderDifferenceTypeColor}"));
                    }

                    table.Cell().Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(TextDescriptorFromFormattableString($"{stat.Count:@HeaderDifferenceNumberColor}"));

                    oldStat = stat;
                }
            });
        }



        protected IContainer HeaderCellStyle(IContainer container) => DefaultCellStyle(container, Results.Options.AppSettings.ColorOptions.GetColor("TableHeaderColor"));
        protected IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Results.Options.AppSettings.ColorOptions.GetColor("TableCellColor"));

        protected IContainer DefaultCellStyle(IContainer container, Color backgroundColor)
        {
            return container.Border(1)
                .BorderColor(Results.Options.AppSettings.ColorOptions.GetColor("TableBorderColor"))
                .Background(backgroundColor)
                .PaddingVertical(2)
                .PaddingHorizontal(2)
                .AlignMiddle();
        }



        protected IContainer DifferenceHeaderCellStyle(IContainer container) => DifferenceCellStyle(container, Results.Options.AppSettings.ColorOptions.GetColor("TableDifferenceHeaderColor"));
        protected IContainer DifferenceCellStyle(IContainer container) => DifferenceCellStyle(container, Results.Options.AppSettings.ColorOptions.GetColor("TableDifferenceCellColor"));
        protected IContainer DifferenceCellStyle(IContainer container, Color backgroundColor)
        {
            return container.Border(1)
                .BorderColor(Results.Options.AppSettings.ColorOptions.GetColor("TableDifferenceBorderColor"))
                .Background(backgroundColor)
                .PaddingVertical(2)
                .PaddingHorizontal(2)
                .AlignMiddle();
        }




        protected void AddEcuComparisonPage(ColumnDescriptor column, List<Action<TextDescriptor>> headerTexts, EcuComparisonResult ecuComparison)
        {
            var options = Results.Options;

            if (ecuComparison.MasterEcuDataMissingInFirst != null && ecuComparison.MasterEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU MISSING IN FIRST FILE ({ecuComparison.MasterEcuDataMissingInFirst.Count:@HeaderDifferenceNumberColor})") }, ecuComparison.MasterEcuDataMissingInFirst);
            if (ecuComparison.MasterEcuDataMissingInSecond != null && ecuComparison.MasterEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU MISSING IN SECOND FILE ({ecuComparison.MasterEcuDataMissingInSecond.Count:@HeaderDifferenceNumberColor})") }, ecuComparison.MasterEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU SETTING DIFFERENCES ({ecuComparison.MasterEcuDataComparisonResult.Count:@HeaderDifferenceNumberColor} TYPES)") }, ecuComparison.MasterEcuDataComparisonResult);

            if (ecuComparison.SubsystemEcuDataMissingInFirst != null && ecuComparison.SubsystemEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU MISSING IN FIRST FILE ({ecuComparison.SubsystemEcuDataMissingInFirst.Count:@HeaderDifferenceNumberColor})") }, ecuComparison.SubsystemEcuDataMissingInFirst);
            if (ecuComparison.SubsystemEcuDataMissingInSecond != null && ecuComparison.SubsystemEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU MISSING IN SECOND FILE ({ecuComparison.SubsystemEcuDataMissingInSecond.Count:@HeaderDifferenceNumberColor})") }, ecuComparison.SubsystemEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU SETTINGS DIFFERENCES ({ecuComparison.SubsystemEcuDataComparisonResult.Count:@HeaderDifferenceNumberColor})") }, ecuComparison.SubsystemEcuDataComparisonResult);
        }


        // {(?<index>\d+):?(?<format>[^}]+)?}
        protected Regex FormatRegex { get; set; } = new Regex("{(?<index>\\d+):?(?<format>[^}]+)?}");
        protected Regex SplitRegex { get; set; } = new Regex("({.+?})");

        protected Action<TextDescriptor> TextDescriptorFromFormattableString(FormattableString formattableString)
        {
            return (text) =>
            {
                var args = formattableString.GetArguments();

                foreach (var s in SplitRegex.Split(formattableString.Format))
                {
                    var m = FormatRegex.Match(s);
                    if (!m.Success)
                        text.Span(s).Bold();
                    else
                    {
                        var format = m.Groups["format"].Value;
                        String argValue = args[Convert.ToInt32(m.Groups["index"].Value)].ToString();
                        Color? color = null;

                        if (!String.IsNullOrEmpty(format))
                        {
                            if (format.StartsWith("#"))
                                color = Color.FromHex(format);
                            else if (format.StartsWith("@"))
                                color = Color.FromHex(Results.Options.AppSettings.ColorOptions.GetColor(format.Substring(1)));
                            else
                            {
                                argValue = String.Format("{0}", format, args[Convert.ToInt32(m.Groups["index"].Value)]);
                            }
                        }

                        text.Span(argValue).Bold().FontColor(color ?? Results.Options.AppSettings.ColorOptions.GetColor("DefaultTextColor"));
                    }
                }
            };
        }




        protected void AddEcuDataComparison(ColumnDescriptor column, List<Action<TextDescriptor>> mainHeaderTexts, List<EcuDataComparisonResult> comparisonResults)
        {
            List<Action<TextDescriptor>> headerTextsDifferences = null;
            List<Action<TextDescriptor>> headerTextMissingFirst = null;
            List<Action<TextDescriptor>> headerTextMissingSecond = null;
            var options = Results.Options;


            int i = 1;
            foreach (var comparisonResult in comparisonResults)
            {
                column.Spacing(10);

                if (comparisonResult.Path[1] == "subsystem")
                {
                    var EcuIdType = EcuComparisonResult.ExtractSubsystemEcuAndTypeFromKey(comparisonResult.Path[2]);
                    if (!String.IsNullOrEmpty(EcuIdType.EcuId))
                    {
                        // subsystem case
                        headerTextsDifferences = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i}: {EcuIdType.EcuId:@HeaderECUColor}"), TextDescriptorFromFormattableString($"TYPE: {EcuIdType.Type:@HeaderTypeColor} ({comparisonResult.Differences.Count:@HeaderDifferenceNumberColor} DIFFERENCES)") };
                        headerTextMissingFirst = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i}: {EcuIdType.EcuId:@HeaderECUColor}"), TextDescriptorFromFormattableString($"TYPE: {EcuIdType.Type:@HeaderTypeColor} ({comparisonResult.FieldsMissingInFirst.Count:@HeaderDifferenceNumberColor} FIELDS MISSING IN FIRST FILE)") };
                        headerTextMissingSecond = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i++}: {EcuIdType.EcuId:@HeaderECUColor}"), TextDescriptorFromFormattableString($"TYPE: {EcuIdType.Type:@HeaderTypeColor} ({comparisonResult.FieldsMissingInSecond.Count:@HeaderDifferenceNumberColor} FIELDS MISSING IN SECOND FILE)") };
                    }
                }
                else
                {
                    // master case
                    headerTextsDifferences = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i}: {comparisonResult.Path[1]:@HeaderTypeColor} ({comparisonResult.Differences.Count:@HeaderDifferenceNumberColor} DIFFERENCES)") };
                    headerTextMissingFirst = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i}: {comparisonResult.Path[1]:@HeaderTypeColor} ({comparisonResult.FieldsMissingInFirst.Count:@HeaderDifferenceNumberColor} FIELDS MISSING IN FIRST FILE)") };
                    headerTextMissingSecond = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i++}: {comparisonResult.Path[1]:@HeaderTypeColor} ({comparisonResult.FieldsMissingInSecond.Count:@HeaderDifferenceNumberColor} FIELDS MISSING IN SECOND FILE)") };
                }


                if (comparisonResult.FieldsMissingInFirst != null && comparisonResult.FieldsMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                    AddValueItemDataOnContainer(column.Item().MinHeight(300f), headerTextMissingFirst, comparisonResult.FieldsMissingInFirst);

                if (comparisonResult.FieldsMissingInSecond != null && comparisonResult.FieldsMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                    AddValueItemDataOnContainer(column.Item().MinHeight(300f), headerTextMissingSecond, comparisonResult.FieldsMissingInSecond);

                if (comparisonResult.Differences != null && comparisonResult.Differences.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.Differences)))
                    AddDifferencesDataOnContainer(column.Item().MinHeight(300f), headerTextsDifferences, comparisonResult.Differences);
            }

        }



        protected void AddDifferencesDataOnContainer(IContainer container, List<Action<TextDescriptor>> headerTexts, List<DifferenceMessage> differences)
        {
            var options = Results.Options;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (var s in headerTexts)
                    {
                        header.Cell().ColumnSpan(10).Element(DifferenceHeaderCellStyle).AlignCenter().Text(s);
                    }
                    header.Cell().ColumnSpan(10).Element(DifferenceHeaderCellStyle).AlignCenter().Text("Path").Bold();
                    //header.Cell().ColumnSpan(3).Element(DifferenceHeaderCellStyle).Text("Description 2").Bold();
                    //header.Cell().ColumnSpan(2).Element(DifferenceHeaderCellStyle).Text("Property").Bold();
                    header.Cell().ColumnSpan(5).Element(DifferenceHeaderCellStyle).AlignCenter().Text("Value 1").Bold();
                    header.Cell().ColumnSpan(5).Element(DifferenceHeaderCellStyle).AlignCenter().Text("Value 2").Bold();
                });

                int i = 1;
                foreach (var difference in differences)
                {
                    if (!options.CheckEnumerableOption(options.BypassFields, difference.FieldProperty))
                    {
                        table.Cell().ColumnSpan(10).Element(DifferenceCellStyle).AlignLeft().Shrink().ShowEntire().Text($"{i++}: {difference.GetPathDisplayString()}").Bold();
                        //table.Cell().ColumnSpan(3).Element(DifferenceCellStyle).Shrink().ShowEntire().Text(difference.FieldDescriptions.Count > 1 ? difference.FieldDescriptions[1] : String.Empty);
                        //table.Cell().ColumnSpan(2).Element(DifferenceCellStyle).Shrink().ShowEntire().Text(JsonSerializer.Serialize(difference.FieldProperty, Options.JsonSerializerOptions));
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.FirstValue, difference.SecondValue, difference.FieldParameters, Results.Options.AppSettings.ColorOptions.GetColor("DifferenceLeftCharacterColor")));
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.SecondValue, difference.FirstValue, difference.FieldParameters, Results.Options.AppSettings.ColorOptions.GetColor("DifferenceRightCharacterColor")));
                    }
                }
            });
        }


        protected Action<TextDescriptor> AddColoredText(String value1, String value2, FieldParametersEnum fieldParameters, Color color)
        {
            return (text) =>
            {
                if (!fieldParameters.HasFlag(FieldParametersEnum.IsFreeText) || fieldParameters.HasFlag(FieldParametersEnum.IsNumerical))
                    text.Span(value1).FontColor(value1 != value2 ? color : Results.Options.AppSettings.ColorOptions.GetColor("DefaultTextColor"));
                else
                {
                    for (int i = 0; i < value1?.Length; i++)
                    {
                        if (i >= (value2?.Length ?? 0))
                        {
                            text.Span(value1[i].ToString()).FontColor(Results.Options.AppSettings.ColorOptions.GetColor("DifferenceAdditionalColor"));
                        }
                        else
                        {
                            var valueChar = value1[i];
                            if (valueChar == value2[i])
                                text.Span(valueChar.ToString()).FontColor(Results.Options.AppSettings.ColorOptions.GetColor("DefaultTextColor"));
                            else
                                text.Span(valueChar.ToString()).FontColor(color);
                        }
                    }
                }
            };
        }



        protected void AddMissingEcuData(ColumnDescriptor column, List<Action<TextDescriptor>> mainHeaderTexts, Dictionary<String, EcuData> missingEcuData)
        {
            column.Spacing(10);

            List<Action<TextDescriptor>> headerTexts = null;

            int i = 1;
            foreach (var ecuData in missingEcuData)
            {
                var EcuIdType = EcuComparisonResult.ExtractSubsystemEcuAndTypeFromKey(ecuData.Key);
                if (!String.IsNullOrEmpty(EcuIdType.EcuId))
                {
                    // subsystem case
                    headerTexts = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i++}: {EcuIdType.EcuId:@HeaderECUColor}"), TextDescriptorFromFormattableString($"TYPE: {EcuIdType.Type:@HeaderTypeColor} ({ecuData.Value.Values.Count:@HeaderDifferenceNumberColor} MISSING)") };
                }
                else
                {
                    // master case
                    headerTexts = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE: {ecuData.Key:@HeaderTypeColor} ({ecuData.Value.Values.Count:@HeaderDifferenceNumberColor} MISSING)") };
                }


                AddValueItemDataOnContainer(column.Item().MinHeight(300f), headerTexts, ecuData.Value.Values.Dictionary);
            }
        }



        protected void AddValueItemDataOnContainer(IContainer container, List<Action<TextDescriptor>> headerTexts, Dictionary<String, ValueItem> valueItems)
        {
            if (valueItems.Count > 0)
            {
                container.Table(table =>
                 {
                     table.ColumnsDefinition(columns =>
                     {
                         columns.RelativeColumn();
                         columns.RelativeColumn();
                     });

                     table.Header(header =>
                     {
                         foreach (var s in headerTexts)
                         {
                             header.Cell().ColumnSpan(2).Element(HeaderCellStyle).AlignCenter().Text(s);
                         }
                         header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Name").Bold();
                         header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Value").Bold();
                     });

                     int i = 1;
                     foreach (var valueItem in valueItems.Values)
                     {
                         if (valueItem.SubValues?.Count > 0)
                         {
                             table.Cell().Element(CellStyle).AlignLeft().Shrink().ShowEntire().Text($"{i++}: {valueItem.GetName()}").Bold();
                             AddValueItemDataOnContainer(table.Cell().Element(CellStyle).AlignCenter(), new List<Action<TextDescriptor>>(), valueItem.SubValues.Dictionary);
                         }
                         else
                         {
                             table.Cell().Element(CellStyle).AlignLeft().Shrink().ShowEntire().Text($"{i++}: {valueItem.GetName()}").Bold();
                             table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(valueItem.GetValue());
                         }
                     }
                 });
            }
        }


        protected void AddMissingEcusNewPage(ColumnDescriptor column, Action<TextDescriptor> headerText, Dictionary<String, Ecu> missingEcus)
        {
            column.Spacing(10);

            column.Item().MinHeight(300f).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.1f);
                        columns.RelativeColumn(0.4f);
                        columns.RelativeColumn(0.25f);
                        columns.RelativeColumn(0.25f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().ColumnSpan(4).Element(HeaderCellStyle).AlignCenter().Text(headerText);
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Id").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Name").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("LogicalLink").Bold();
                        header.Cell().Element(HeaderCellStyle).AlignCenter().Text("OdxVariant").Bold();
                    });


                    foreach (var ecu in missingEcus.Values)
                    {
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(TextDescriptorFromFormattableString($"{ecu.EcuId:@HeaderECUColor}"));
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(TextDescriptorFromFormattableString($"{ecu.EcuName:@HeaderECUColor}"));
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(ecu.LogicalLink);
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(ecu.TesterOdxVariant);
                    }

                });
        }



        protected void NewPage(PageDescriptor page)
        {
            var options = Results.Options;

            var inputFiles = options.Inputs.ToArray();

            page.Size(PageSizes.A4.Landscape());
            page.MarginVertical(1.0f, Unit.Centimetre);
            page.MarginHorizontal(1.5f, Unit.Centimetre);
            page.PageColor(Results.Options.AppSettings.ColorOptions.GetColor("PageColor"));
            page.DefaultTextStyle(x => x.FontSize(14).FontColor(Results.Options.AppSettings.ColorOptions.GetColor("DefaultTextColor")));

            //page.Header()
            //    .Column(column =>
            //    {
            //        column.Item().AlignLeft().AlignMiddle()
            //        .Text($"1: {Path.GetFileName(inputFiles[0])}")
            //        .FontSize(10);

            //        column.Item().AlignLeft().AlignMiddle()
            //        .Text($"2: {Path.GetFileName(inputFiles[1])}")
            //        .FontSize(10);
            //    });

            page.Header()
                .Row(row =>
                {
                    row.RelativeItem().AlignLeft().AlignMiddle()
                    .Text($"1: {Path.GetFileName(inputFiles[0])}")
                    .FontSize(12);

                    row.RelativeItem().AlignRight().AlignMiddle()
                    .Text($"2: {Path.GetFileName(inputFiles[1])}")
                    .FontSize(12);
                });

            page.Footer()
                .Row(row =>
                {
                    row.RelativeItem(0.6f).AlignLeft().AlignMiddle()
                    .Text(Results.Timestamp.ToString("G")).FontSize(14);

                    row.RelativeItem(0.4f).AlignRight().AlignMiddle()
                    .Text(td => td.CurrentPageNumber().FontSize(14));
                });
        }
    }
}
