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
                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile))
                            AddMissingEcusNewPage(column, $"ECUs MISSING IN FIRST FILE ({Results.EcusMissingInFirst.Count})", Results.EcusMissingInFirst);

                        if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile))
                            AddMissingEcusNewPage(column, $"ECUs MISSING IN SECOND FILE ({Results.EcusMissingInSecond.Count})", Results.EcusMissingInSecond);

                        foreach (var ecuComparison in Results.EcusComparisonResult)
                        {
                            if (options.CheckEcuIds(ecuComparison.FirstEcu.EcuId))
                            {
                                AddEcuComparisonPage(column, new List<String> { $"ECU: {ecuComparison.FirstEcu.EcuId} ({ecuComparison.FirstEcu.EcuName})", $"ECU: {ecuComparison.SecondEcu.EcuId} ({ecuComparison.SecondEcu.EcuName})" }, ecuComparison);
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


        private void AddEcuComparisonPage(ColumnDescriptor column, List<String> headerTexts, EcuComparisonResult ecuComparison)
        {
            var options = Results.Options;

            if (ecuComparison.MasterEcuDataMissingInFirst != null && ecuComparison.MasterEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<String>(headerTexts) { $"MASTER ECU MISSING IN FIRST FILE ({ecuComparison.MasterEcuDataMissingInFirst.Count})" }, ecuComparison.MasterEcuDataMissingInFirst);
            if (ecuComparison.MasterEcuDataMissingInSecond != null && ecuComparison.MasterEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<String>(headerTexts) { $"MASTER ECU MISSING IN SECOND FILE ({ecuComparison.MasterEcuDataMissingInSecond.Count})" }, ecuComparison.MasterEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<String>(headerTexts) { $"MASTER ECU DATA DIFFERENCES ({ecuComparison.MasterEcuDataComparisonResult.Count} TYPES)" }, ecuComparison.MasterEcuDataComparisonResult);

            if (ecuComparison.SubsystemEcuDataMissingInFirst != null && ecuComparison.SubsystemEcuDataMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                AddMissingEcuData(column, new List<String>(headerTexts) { $"SUBSYSTEMS ECU MISSING IN FIRST FILE ({ecuComparison.SubsystemEcuDataMissingInFirst.Count})" }, ecuComparison.SubsystemEcuDataMissingInFirst);
            if (ecuComparison.SubsystemEcuDataMissingInSecond != null && ecuComparison.SubsystemEcuDataMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                AddMissingEcuData(column, new List<String>(headerTexts) { $"SUBSYSTEMS ECU MISSING IN SECOND FILE ({ecuComparison.SubsystemEcuDataMissingInSecond.Count})" }, ecuComparison.SubsystemEcuDataMissingInSecond);

            AddEcuDataComparison(column, new List<String>(headerTexts) { $"SUBSYSTEMS ECU DATA DIFFERENCES ({ecuComparison.SubsystemEcuDataComparisonResult.Count})" }, ecuComparison.SubsystemEcuDataComparisonResult);
        }


        protected void AddEcuDataComparison(ColumnDescriptor column, List<String> mainHeaderTexts, List<EcuDataComparisonResult> comparisonResults)
        {
            List<String> headerTextsDifferences = null;
            List<String> headerTextMissingFirst = null;
            List<String> headerTextMissingSecond = null;
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
                        headerTextsDifferences = new List<string>(mainHeaderTexts) { $"SUBSYSTEM #{i}: {match.Groups["subsystem"].Value}", $"TYPE: {match.Groups["type"].Value} ({comparisonResult.Differences.Count} DIFFERENCES)" };
                        headerTextMissingFirst = new List<string>(mainHeaderTexts) { $"SUBSYSTEM #{i}: {match.Groups["subsystem"].Value}", $"TYPE: {match.Groups["type"].Value} ({comparisonResult.FieldsMissingInFirst.Count} FIELDS MISSING IN FIRST FILE)" };
                        headerTextMissingSecond = new List<string>(mainHeaderTexts) { $"SUBSYSTEM #{i++}: {match.Groups["subsystem"].Value}", $"TYPE: {match.Groups["type"].Value} ({comparisonResult.FieldsMissingInSecond.Count} FIELDS MISSING IN SECOND FILE)" };
                    }
                }
                else
                {
                    // master case
                    headerTextsDifferences = new List<string>(mainHeaderTexts) { $"TYPE #{i}: {comparisonResult.Path[1]} ({comparisonResult.Differences.Count} DIFFERENCES)" };
                    headerTextMissingFirst = new List<string>(mainHeaderTexts) { $"TYPE #{i}: {comparisonResult.Path[1]} ({comparisonResult.FieldsMissingInFirst.Count} FIELDS MISSING IN FIRST FILE)" };
                    headerTextMissingSecond = new List<string>(mainHeaderTexts) { $"TYPE #{i++}: {comparisonResult.Path[1]} ({comparisonResult.FieldsMissingInSecond.Count} FIELDS MISSING IN SECOND FILE)" };
                }


                if (comparisonResult.FieldsMissingInFirst != null && comparisonResult.FieldsMissingInFirst.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                    AddValueItemDataOnContainer(column.Item(), headerTextMissingFirst, comparisonResult.FieldsMissingInFirst);

                if (comparisonResult.FieldsMissingInSecond != null && comparisonResult.FieldsMissingInSecond.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                    AddValueItemDataOnContainer(column.Item(), headerTextMissingSecond, comparisonResult.FieldsMissingInSecond);

                if (comparisonResult.Differences != null && comparisonResult.Differences.Any() && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.Differences)))
                    AddDifferencesDataOnContainer(column.Item(), headerTextsDifferences, comparisonResult.Differences);

                column.Item().PageBreak();
            }

        }



        protected void AddDifferencesDataOnContainer(IContainer container, List<String> headerTexts, List<DifferenceMessage> differences)
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
                        header.Cell().ColumnSpan(10).Element(DifferenceHeaderCellStyle).AlignCenter().Text(s).Bold();
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
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.FirstValue, difference.SecondValue, difference.FieldParameters, Colors.Red.Medium));
                        table.Cell().ColumnSpan(5).Element(DifferenceCellStyle).AlignCenter().Shrink().ShowEntire().Text(AddColoredText(difference.SecondValue, difference.FirstValue, difference.FieldParameters, Colors.Green.Medium));
                    }
                }
            });
        }


        protected Action<TextDescriptor> AddColoredText(String value1, String value2, FieldParametersEnum fieldParameters, Color color)
        {
            return (text) =>
            {
                if (!fieldParameters.HasFlag(FieldParametersEnum.IsFreeText))
                    text.Span(value1).FontColor(value1 != value2 ? color : Colors.Black);
                else
                {
                    for (int i = 0; i < value1?.Length; i++)
                    {
                        if (i >= (value2?.Length ?? 0))
                        {
                            text.Span(value1[i].ToString()).FontColor(Colors.Blue.Medium);
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



        protected void AddMissingEcuData(ColumnDescriptor column, List<String> mainHeaderTexts, Dictionary<String, EcuData> missingEcuData)
        {
            column.Spacing(10);

            List<String> headerTexts = null;

            int i = 1;
            foreach (var ecuData in missingEcuData)
            {
                var match = SubsystemDataKey.Match(ecuData.Key);
                if (match.Success)
                {
                    // subsystem case
                    headerTexts = new List<string>(mainHeaderTexts) { $"SUBSYSTEM #{i++}: {match.Groups["subsystem"].Value}", $"TYPE: {match.Groups["type"].Value}" };
                }
                else
                {
                    // master case
                    headerTexts = new List<string>(mainHeaderTexts) { $"ECU #{i++}: {ecuData.Value.DisplayName ?? ecuData.Value.TiName}", $"TYPE: {ecuData.Key}" };
                }


                AddValueItemDataOnContainer(column.Item(), headerTexts, ecuData.Value.Values.Dictionary);

                column.Item().PageBreak();
            }
        }



        protected void AddValueItemDataOnContainer(IContainer container, List<String> headerTexts, Dictionary<String, ValueItem> valueItems)
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
                             header.Cell().ColumnSpan(2).Element(HeaderCellStyle).AlignCenter().Text(s).Bold();
                         }
                         header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Name").Bold();
                         header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Value").Bold();
                     });

                     int i = 1;
                     foreach (var valueItem in valueItems.Values)
                     {
                         if (valueItem.SubValues?.Count > 0)
                         {
                             table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text($"{i++}: {valueItem.GetName()}").Bold();
                             AddValueItemDataOnContainer(table.Cell().Element(CellStyle).AlignCenter(), new List<string>(), valueItem.SubValues.Dictionary);
                         }
                         else
                         {
                             table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text($"{i++}: {valueItem.GetName()}").Bold();
                             table.Cell().Element(CellStyle).AlignCenter().Shrink().ShowEntire().Text(valueItem.GetValue());
                         }
                     }
                 });
            }
        }


        private void AddMissingEcusNewPage(ColumnDescriptor column, String headerText, Dictionary<String, Ecu> missingEcus)
        {
            column.Spacing(10);

            column.Item().Table(table =>
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
                        header.Cell().ColumnSpan(4).Element(HeaderCellStyle).AlignCenter().Text(headerText).Bold();
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

            column.Item().PageBreak();
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
