using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;
using Fededim.OdisBackupCompare.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Logging;

namespace OdisBackupCompare
{
    public class PdfGenerator : IDocument
    {
        protected ComparisonResults Results { get; }
        protected Regex SubsystemDataKey { get; }

        protected ILogger<PdfGenerator> Logger { get; }

        public PdfGenerator(ComparisonResults results, ILogger<PdfGenerator> logger)
        {
            Results = results;
            Logger = logger;
            SubsystemDataKey = new Regex($"(?<subsystem>.+)_(?<type>{String.Join('|', MeaningfulText.RemapData.Values.ToList())})");
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
                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile) && Results.EcusMissingInFirst.Any())
                            AddMissingEcusNewPage(column, TextDescriptorFromFormattableString($"ECUs MISSING IN FIRST FILE ({Results.EcusMissingInFirst.Count:#FFE91E63})"), Results.EcusMissingInFirst);

                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile) && Results.EcusMissingInSecond.Any())
                            AddMissingEcusNewPage(column, TextDescriptorFromFormattableString($"ECUs MISSING IN SECOND FILE ({Results.EcusMissingInSecond.Count:#FFE91E63})"), Results.EcusMissingInSecond);

                        foreach (var ecuComparison in Results.EcusComparisonResult)
                        {
                            if (options.CheckEcuIds(ecuComparison.FirstEcu.EcuId))
                            {
                                AddEcuComparisonPage(column, new List<Action<TextDescriptor>> { TextDescriptorFromFormattableString($"ECU: {ecuComparison.FirstEcu.EcuId:#FF673AB7} ({ecuComparison.FirstEcu.EcuName:#FF673AB7})"), TextDescriptorFromFormattableString($"ECU: {ecuComparison.SecondEcu.EcuId:#FF673AB7} ({ecuComparison.SecondEcu.EcuName:#FF673AB7})") }, ecuComparison);
                            }
                        }
                    });
                });
        }


        protected IContainer HeaderCellStyle(IContainer container) => DefaultCellStyle(container, Colors.Grey.Lighten3);
        protected IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.White);

        protected IContainer DefaultCellStyle(IContainer container, Color backgroundColor)
        {
            return container.Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(backgroundColor)
                .PaddingVertical(2)
                .PaddingHorizontal(2)
                .AlignMiddle();
        }



        protected IContainer DifferenceHeaderCellStyle(IContainer container) => DifferenceCellStyle(container, Colors.Grey.Lighten3);
        protected IContainer DifferenceCellStyle(IContainer container) => DifferenceCellStyle(container, Colors.White);
        protected IContainer DifferenceCellStyle(IContainer container, Color backgroundColor)
        {
            return container.Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(backgroundColor)
                .PaddingVertical(2)
                .PaddingHorizontal(2)
                .AlignMiddle();
        }




        private void AddEcuComparisonPage(ColumnDescriptor column, List<Action<TextDescriptor>> headerTexts, EcuComparisonResult ecuComparison)
        {
            var options = Results.Options;

            if (ecuComparison.MasterEcuDataMissingInFirst != null && ecuComparison.MasterEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU MISSING IN FIRST FILE ({ecuComparison.MasterEcuDataMissingInFirst.Count:#FFE91E63})") }, ecuComparison.MasterEcuDataMissingInFirst);
            if (ecuComparison.MasterEcuDataMissingInSecond != null && ecuComparison.MasterEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU MISSING IN SECOND FILE ({ecuComparison.MasterEcuDataMissingInSecond.Count:#FFE91E63})") }, ecuComparison.MasterEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"MASTER ECU DATA DIFFERENCES ({ecuComparison.MasterEcuDataComparisonResult.Count:#FFE91E63} TYPES)") }, ecuComparison.MasterEcuDataComparisonResult);

            if (ecuComparison.SubsystemEcuDataMissingInFirst != null && ecuComparison.SubsystemEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU MISSING IN FIRST FILE ({ecuComparison.SubsystemEcuDataMissingInFirst.Count:#FFE91E63})") }, ecuComparison.SubsystemEcuDataMissingInFirst);
            if (ecuComparison.SubsystemEcuDataMissingInSecond != null && ecuComparison.SubsystemEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU MISSING IN SECOND FILE ({ecuComparison.SubsystemEcuDataMissingInSecond.Count:#FFE91E63})") }, ecuComparison.SubsystemEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<Action<TextDescriptor>>(headerTexts) { TextDescriptorFromFormattableString($"SUBSYSTEMS ECU DATA DIFFERENCES ({ecuComparison.SubsystemEcuDataComparisonResult.Count:#FFE91E63})") }, ecuComparison.SubsystemEcuDataComparisonResult);
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
                            else
                            {
                                argValue = String.Format("{0}",format, args[Convert.ToInt32(m.Groups["index"].Value)]);
                            }
                        }

                        text.Span(argValue).Bold().FontColor(color ?? Colors.Black);
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
                    var match = SubsystemDataKey.Match(comparisonResult.Path[2]);
                    if (match.Success)
                    {
                        // subsystem case
                        headerTextsDifferences = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i}: {match.Groups["subsystem"].Value:#FF673AB7}"), TextDescriptorFromFormattableString($"TYPE: {match.Groups["type"].Value:#FFF57C00} ({comparisonResult.Differences.Count:#FFE91E63} DIFFERENCES)") };
                        headerTextMissingFirst = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i}: {match.Groups["subsystem"].Value:#FF673AB7}"), TextDescriptorFromFormattableString($"TYPE: {match.Groups["type"].Value:#FFF57C00} ({comparisonResult.FieldsMissingInFirst.Count:#FFE91E63} FIELDS MISSING IN FIRST FILE)") };
                        headerTextMissingSecond = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i++}: {match.Groups["subsystem"].Value:#FF673AB7}"), TextDescriptorFromFormattableString($"TYPE: {match.Groups["type"].Value:#FFF57C00} ({comparisonResult.FieldsMissingInSecond.Count:#FFE91E63} FIELDS MISSING IN SECOND FILE)") };
                    }
                }
                else
                {
                    // master case
                    headerTextsDifferences = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i}: {comparisonResult.Path[1]:#FFF57C00} ({comparisonResult.Differences.Count:#FFE91E63} DIFFERENCES)") };
                    headerTextMissingFirst = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i}: {comparisonResult.Path[1]:#FFF57C00} ({comparisonResult.FieldsMissingInFirst.Count:#FFE91E63} FIELDS MISSING IN FIRST FILE)") };
                    headerTextMissingSecond = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"TYPE #{i++}: {comparisonResult.Path[1]:#FFF57C00} ({comparisonResult.FieldsMissingInSecond.Count:#FFE91E63} FIELDS MISSING IN SECOND FILE)") };
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
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.FirstValue, difference.SecondValue, difference.FieldParameters, Colors.Red.Darken2));
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.SecondValue, difference.FirstValue, difference.FieldParameters, Colors.Green.Darken2));
                    }
                }
            });
        }


        protected Action<TextDescriptor> AddColoredText(String value1, String value2, FieldParametersEnum fieldParameters, Color color)
        {
            return (text) =>
            {
                if (!fieldParameters.HasFlag(FieldParametersEnum.IsFreeText) || fieldParameters.HasFlag(FieldParametersEnum.IsNumerical))
                    text.Span(value1).FontColor(value1 != value2 ? color : Colors.Black);
                else
                {
                    for (int i = 0; i < value1?.Length; i++)
                    {
                        if (i >= (value2?.Length ?? 0))
                        {
                            text.Span(value1[i].ToString()).FontColor(Colors.Blue.Darken2);
                        }
                        else
                        {
                            var valueChar = value1[i];
                            if (valueChar == value2[i])
                                text.Span(valueChar.ToString()).FontColor(Colors.Black);
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
                var match = SubsystemDataKey.Match(ecuData.Key);
                if (match.Success)
                {
                    // subsystem case
                    headerTexts = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"SUBSYSTEM #{i++}: {match.Groups["subsystem"].Value:#FF673AB7}"), TextDescriptorFromFormattableString($"TYPE: {match.Groups["type"].Value:#FFF57C00}") };
                }
                else
                {
                    // master case
                    headerTexts = new List<Action<TextDescriptor>>(mainHeaderTexts) { TextDescriptorFromFormattableString($"ECU #{i++}: {ecuData.Value.DisplayName ?? ecuData.Value.TiName:#FF673AB7}"), TextDescriptorFromFormattableString($"TYPE: {ecuData.Key:#FFF57C00}") };
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


        private void AddMissingEcusNewPage(ColumnDescriptor column, Action<TextDescriptor> headerText, Dictionary<String, Ecu> missingEcus)
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
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(ecu.EcuId).Bold();
                        table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(ecu.EcuName).Bold();
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
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(14));

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
