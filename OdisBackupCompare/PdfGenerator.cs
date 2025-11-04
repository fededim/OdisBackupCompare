using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OdisBackupCompare
{
    public class PdfGenerator : IDocument
    {
        protected ComparisonResults Results { get; }

        protected Regex SubsystemDataKey { get; }

        public PdfGenerator(ComparisonResults results)
        {
            Results = results;
            SubsystemDataKey = new Regex($"(?<subsystem>.+)_(?<type>{String.Join('|', MeaningfulText.RemapData.Values.ToList())})");
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            var options = Results.Options;

            if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile))
                AddMissingEcusNewPage(container, $"ECUs MISSING IN FIRST FILE ({Results.EcusMissingInFirst.Count})", Results.EcusMissingInFirst);

            if (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile))
                AddMissingEcusNewPage(container, $"ECUs MISSING IN SECOND FILE ({Results.EcusMissingInSecond.Count})", Results.EcusMissingInSecond);


            foreach (var ecuComparison in Results.EcusComparisonResult)
            {
                AddEcuComparisonPage(container, new List<String> { $"ECU: {ecuComparison.FirstEcu.EcuId} ({ecuComparison.FirstEcu.EcuName})", $"ECU: {ecuComparison.SecondEcu.EcuId} ({ecuComparison.SecondEcu.EcuName})" }, ecuComparison);
            }
        }


        protected IContainer HeaderCellStyle(IContainer container) => DefaultCellStyle(container, Colors.Grey.Lighten3);
        protected IContainer CellStyle(IContainer container) => DefaultCellStyle(container, Colors.White);

        protected IContainer DefaultCellStyle(IContainer container, Color backgroundColor)
        {
            return container.Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(backgroundColor)
                .PaddingVertical(5)
                .PaddingHorizontal(10)
                .AlignCenter()
                .AlignMiddle();
        }



        private void AddEcuComparisonPage(IDocumentContainer container, List<String> headerTexts, EcuComparisonResult ecuComparison)
        {
            var options = Results.Options;

            container
                .Page(page =>
                {
                    NewPage(page);
                    page.DefaultTextStyle(x => x.FontSize(16));

                    page.Content().Column(column =>
                    {
                        if (ecuComparison.MasterEcuDataMissingInFirst != null && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                            AddMissingEcuData(column, new List<String>(headerTexts) { $"MASTER ECU DATA MISSING IN FIRST FILE ({ecuComparison.MasterEcuDataMissingInFirst.Count})" }, ecuComparison.MasterEcuDataMissingInFirst);
                        if (ecuComparison.MasterEcuDataMissingInSecond != null && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                            AddMissingEcuData(column, new List<String>(headerTexts) { $"MASTER ECU DATA MISSING IN SECOND FILE ({ecuComparison.MasterEcuDataMissingInSecond.Count})" }, ecuComparison.MasterEcuDataMissingInSecond);

                        if (ecuComparison.SubsystemEcuDataMissingInFirst != null && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInFirstFile)))
                            AddMissingEcuData(column, new List<String>(headerTexts) { $"SUBSYSTEMS ECU DATA MISSING IN FIRST FILE ({ecuComparison.SubsystemEcuDataMissingInFirst.Count})" }, ecuComparison.SubsystemEcuDataMissingInFirst);
                        if (ecuComparison.SubsystemEcuDataMissingInSecond != null && (!options.ComparisonOptions.Any() || options.ComparisonOptions.Contains(ComparisonOptionsEnum.DataMissingInSecondFile)))
                            AddMissingEcuData(column, new List<String>(headerTexts) { $"SUBSYSTEMS ECU DATA MISSING IN SECOND FILE ({ecuComparison.SubsystemEcuDataMissingInSecond.Count})" }, ecuComparison.SubsystemEcuDataMissingInSecond);
                    });
                });
        }


        protected void AddEcuDataComparison(ColumnDescriptor column, List<String> mainHeaderTexts, List<EcuDataComparisonResult> comparisonResults)
        {
            List<String> headerTexts = null;

            int i = 1;
            foreach (var comparisonResult in comparisonResults)
            {
                var match = SubsystemDataKey.Match(comparisonResults.Key);
                if (match.Success)
                {
                    // subsystem case
                    headerTexts = new List<string>(mainHeaderTexts) { $"SUBSYSTEM #{i++}: {match.Groups["subsystem"].Value}", $"TYPE: {match.Groups["type"].Value}" };
                }
                else
                {
                    // master case
                    headerTexts = new List<string>(mainHeaderTexts) { $"ECU #{i++}: {comparisonResults.Value.DisplayName ?? comparisonResults.Value.TiName}", $"TYPE: {comparisonResults.Key}" };
                }

                column.Spacing(10);

                AddValueItemDataOnContainer(column.Item(), headerTexts, comparisonResults.Value.Values);
            }

            column.Item().PageBreak();
        }



        protected void AddMissingEcuData(ColumnDescriptor column, List<String> mainHeaderTexts, Dictionary<String, EcuData> missingEcuData)
        {
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

                column.Spacing(10);

                AddValueItemDataOnContainer(column.Item(), headerTexts, ecuData.Value.Values);
            }

            column.Item().PageBreak();
        }



        protected void AddValueItemDataOnContainer(IContainer container, List<String> headerTexts, DictionaryList<ValueItem, String> valueItems)
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
                         header.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text(s).Bold();
                     }
                     header.Cell().Element(HeaderCellStyle).Text("Name").Bold();
                     header.Cell().Element(HeaderCellStyle).Text("Value").Bold();
                 });


                 foreach (var valueItem in valueItems)
                 {
                     if (valueItem.SubValues?.Count > 0)
                     {
                         table.Cell().Element(CellStyle).ShowEntire().Text(valueItem.GetName());
                         AddValueItemDataOnContainer(table.Cell().Element(CellStyle).ShowEntire(), new List<string>(), valueItem.SubValues);
                     }
                     else
                     {
                         table.Cell().Element(CellStyle).ShowEntire().Text(valueItem.GetName());
                         table.Cell().Element(CellStyle).ShowEntire().Text(valueItem.GetValue());
                     }
                 }
             });
        }


        private void AddMissingEcusNewPage(IDocumentContainer container, String headerText, Dictionary<String, Ecu> missingEcus)
        {
            container
                .Page(page =>
                {
                    NewPage(page);
                    page.DefaultTextStyle(x => x.FontSize(16));
                    page.Content().Table(table =>
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
                            header.Cell().ColumnSpan(4).Element(HeaderCellStyle).Text(headerText).Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Id").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Name").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("LogicalLink").Bold();
                            header.Cell().Element(HeaderCellStyle).Text("OdxVariant").Bold();
                        });


                        foreach (var ecu in missingEcus.Values)
                        {
                            table.Cell().Element(CellStyle).ShowEntire().Text(ecu.EcuId);
                            table.Cell().Element(CellStyle).ShowEntire().Text(ecu.EcuName);
                            table.Cell().Element(CellStyle).ShowEntire().Text(ecu.LogicalLink);
                            table.Cell().Element(CellStyle).ShowEntire().Text(ecu.TesterOdxVariant);
                        }
                    });
                });
        }

        private void NewPage(PageDescriptor page)
        {
            var options = Results.Options;

            var inputFiles = options.Inputs.ToArray();

            page.Size(PageSizes.A4.Landscape());
            page.MarginVertical(2.0f, Unit.Centimetre);
            page.MarginHorizontal(2.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(20));

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
                    .FontSize(10);

                    row.RelativeItem().AlignRight().AlignMiddle()
                    .Text($"2: {Path.GetFileName(inputFiles[1])}")
                    .FontSize(10);
                });

            page.Footer()
                .Row(row =>
                {
                    row.RelativeItem(0.6f).AlignLeft().AlignMiddle()
                    .Text(Results.Timestamp.ToString("G")).FontSize(12);

                    row.RelativeItem(0.4f).AlignRight().AlignMiddle()
                    .Text(td => td.CurrentPageNumber().FontSize(12));
                });
        }
    }
}
