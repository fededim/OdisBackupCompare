using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OdisBackupCompare
{
    public enum OutputFileFormatEnum { JSON, PDF };

    public enum ComparisonOptionsEnum { Differences, DataMissingInFirstFile, DataMissingInSecondFile };

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

    public class OdisDataComparerSettings
    {
        public List<FieldPropertyEnum> FieldToBypassOnComparison { get; init; }

        public OdisDataComparerSettings()
        {
            FieldToBypassOnComparison = new List<FieldPropertyEnum>();
        }
    }


    public class OdisDataComparer
    {
        protected Options Settings { get; set; }


        public OdisDataComparer(Options settings)
        {
            Settings = settings ?? throw new ArgumentNullException("settings");
        }


        public static ICollection<String> EmptyStringList = new List<String> { String.Empty };

        public ComparisonResults CompareEcus(Dictionary<String, Ecu> firstSet, Dictionary<String, Ecu> secondSet)
        {
            var result = new ComparisonResults(Settings);

            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                result.EcusMissingInFirst = secondSet.Where(kvp => secondSet.Keys.Except(firstSet.Keys).Contains(kvp.Key)).ToDictionary();
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                result.EcusMissingInSecond = firstSet.Where(kvp => firstSet.Keys.Except(secondSet.Keys).Contains(kvp.Key)).ToDictionary();

            foreach (var ecuId in firstSet.Keys.Intersect(secondSet.Keys))
            {
                if (Settings.CheckEnumerableOption(Settings.EcuIds, ecuId))
                {

                    var firstEcu = firstSet[ecuId];
                    var secondEcu = secondSet[ecuId];

                    var ecuComparisonResult = CompareEcu(firstEcu, secondEcu);
                    if (!ecuComparisonResult.IsEmpty)
                        result.EcusComparisonResult.Add(ecuComparisonResult);
                }
            }

            return result;
        }


        public EcuComparisonResult CompareEcu(Ecu first, Ecu second)
        {
            if (first.EcuId != second.EcuId)
                throw new InvalidDataException($"ECU_ID: {first.EcuId} is different from {second.EcuId}!");

            var ecuComparisonResult = new EcuComparisonResult();

            ecuComparisonResult.EcuId = first.EcuId;
            ecuComparisonResult.FirstEcu = first;
            ecuComparisonResult.SecondEcu = second;

            // COMPARE MASTER DATA
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                ecuComparisonResult.MasterEcuDataMissingInFirst = second.EcuMasters.FilterByPredicate(kvp => second.EcuMasters.Dictionary.Keys.Except(first.EcuMasters.Dictionary.Keys ?? EmptyStringList).Contains(kvp.Key), MeaningfulText.Map);
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                ecuComparisonResult.MasterEcuDataMissingInSecond = first.EcuMasters.FilterByPredicate(kvp => first.EcuMasters.Dictionary.Keys.Except(second.EcuMasters.Dictionary.Keys ?? EmptyStringList).Contains(kvp.Key), MeaningfulText.Map);

            // for the master ecu (it is just one) the keys here ident, adaption_read, coding_read
            foreach (var ecuMasterType in first.EcuMasters.Dictionary.Keys.Intersect(second.EcuMasters.Dictionary.Keys))
            {
                var firstData = first.EcuMasters.Dictionary[ecuMasterType];
                var secondData = second.EcuMasters.Dictionary[ecuMasterType];

                var compareEcuDataResult = CompareEcuData(new List<String> { first.EcuId, MeaningfulText.Map(ecuMasterType) }, null, firstData, secondData);
                if (!compareEcuDataResult.IsEmpty)
                    ecuComparisonResult.MasterEcuDataComparisonResult.Add(compareEcuDataResult);
            }

            // COMPARE SUBSYSTEM DATA
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                ecuComparisonResult.SubsystemEcuDataMissingInFirst = second.EcuSubsystems?.Subsystems?.FilterByPredicate(kvp => second.EcuSubsystems?.Subsystems?.Dictionary?.Keys?.Except(first.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList)?.Contains(kvp.Key) ?? false, MeaningfulText.Map);
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                ecuComparisonResult.SubsystemEcuDataMissingInSecond = first.EcuSubsystems?.Subsystems?.FilterByPredicate(kvp => first.EcuSubsystems?.Subsystems?.Dictionary?.Keys?.Except(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList)?.Contains(kvp.Key) ?? false, MeaningfulText.Map);

            // for the subsystem ecus (they can be more than one) the keys here ident_<display_value of the value with ti_name MAS01171 (subsystem number)>, adaption_read_<ti_name> or coding_read_<ti_name>
            var commonSubsystems = (second.EcuSubsystems?.Subsystems?.Dictionary?.Keys != null) ? first.EcuSubsystems?.Subsystems?.Dictionary?.Keys.Intersect(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys) : null;
            if (commonSubsystems != null)
            {
                foreach (var ecuSubsystemType in commonSubsystems)
                {
                    var firstData = first.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];
                    var secondData = second.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];

                    var descriptions = (String.IsNullOrWhiteSpace(firstData.TiName) && String.IsNullOrWhiteSpace(secondData.TiName)) ?
                        new List<String> { $"{firstData.Values?.FirstOrDefault(v => v.TiName == "IDE00013")?.DisplayValue} - {firstData.Values?.FirstOrDefault(v => v.TiName == "IDE00007")?.DisplayValue}", $"{secondData.Values?.FirstOrDefault(v => v.TiName == "IDE00013")?.DisplayValue} - {secondData.Values?.FirstOrDefault(v => v.TiName == "IDE00007")?.DisplayValue}" } :
                        new List<String> { firstData.TiName, secondData.TiName };

                    var compareEcuDataResult = CompareEcuData(new List<String> { first.EcuId, "subsystem", MeaningfulText.Map(ecuSubsystemType) }, descriptions, firstData, secondData);
                    if (!compareEcuDataResult.IsEmpty)
                        ecuComparisonResult.SubsystemEcuDataComparisonResult.Add(compareEcuDataResult);
                }
            }

            return ecuComparisonResult;
        }



        public EcuDataComparisonResult CompareEcuData(List<String> mainpath, List<String> descriptions, EcuData first, EcuData second)
        {
            var path = new List<String>(mainpath) { first.TiName };

            var result = new EcuDataComparisonResult { First = first, Second = second, Path = path, Descriptions = descriptions };

            if (first.TiName != second.TiName)
                throw new InvalidDataException($"TI_NAME: {first.TiName} is different from {second.TiName}!");

            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.Differences))
                CompareStrings(result.Differences, path, new List<String>(mainpath) { first.DisplayName, second.DisplayName }, FieldPropertyEnum.DisplayName, first.DisplayName, second.DisplayName);

            CompareValueItem(result, mainpath, first?.Values, second?.Values);

            return result;
        }



        public void CompareValueItem(EcuDataComparisonResult result, List<String> mainpath, DictionaryList<ValueItem, String> first, DictionaryList<ValueItem, String> second)
        {
            // Compare values
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInFirstFile))
                result.FieldsMissingInFirst.AddRange(second?.FilterByPredicate(kvp => second?.Dictionary?.Keys.Except(first?.Dictionary?.Keys ?? EmptyStringList).Contains(kvp.Key) ?? false));
            if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.DataMissingInSecondFile))
                result.FieldsMissingInSecond.AddRange(first?.FilterByPredicate(kvp => first?.Dictionary?.Keys.Except(second?.Dictionary?.Keys ?? EmptyStringList).Contains(kvp.Key) ?? false));

            // keys here are actual values
            foreach (var valueItemKey in first.Dictionary.Keys.Intersect(second.Dictionary.Keys))
            {
                var firstValue = first.Dictionary[valueItemKey];
                var secondValue = second.Dictionary[valueItemKey];

                var path = new List<String>(mainpath) { valueItemKey };
                var fieldDescriptions = new List<String>() { firstValue.DisplayName, secondValue.DisplayName };

                if (Settings.CheckEnumerableOption(Settings.ComparisonOptions, ComparisonOptionsEnum.Differences))
                {
                    CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.TiName, firstValue.TiName, secondValue.TiName);
                    CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.TiUnit, firstValue.TiUnit, secondValue.TiUnit);
                    CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.DisplayName, firstValue.DisplayName, secondValue.DisplayName);
                    CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.DisplayValue, firstValue.DisplayValue, secondValue.DisplayValue);
                    CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.DisplayUnit, firstValue.DisplayUnit, secondValue.DisplayUnit);
                    if (!CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.BinValue, firstValue.BinValue, secondValue.BinValue))
                        if (!CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.HexValue, firstValue.HexValue, secondValue.HexValue))
                            CompareStrings(result.Differences, path, fieldDescriptions, FieldPropertyEnum.TiValue, firstValue.TiValue, secondValue.TiValue);
                }

                // perform recursion of subvalues
                if (firstValue.SubValues?.Count > 0 || secondValue.SubValues?.Count > 0)
                    CompareValueItem(result, path, firstValue?.SubValues, secondValue?.SubValues);
            }

        }



        public bool CompareStrings(List<DifferenceMessage> errors, List<String> path, List<String> fieldDescriptions, FieldPropertyEnum fieldProperty, String first, String second)
        {
            if (String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second))
                return false;

            if (!Settings.CheckEnumerableOption(Settings.BypassFields,fieldProperty) && (
                (String.IsNullOrWhiteSpace(first) && !String.IsNullOrWhiteSpace(second)) ||
                (!String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second)) ||
                (first != second)))
            {
                errors.Add(new DifferenceMessage { Path = path, FieldDescriptions = fieldDescriptions, FieldProperty = fieldProperty, Message = $"{first} is different from {second}", FirstValue = first, SecondValue = second });
                return true;
            }

            return false;
        }
    }
}
