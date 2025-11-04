using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OdisBackupCompare
{
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
        protected OdisDataComparerSettings Settings { get; set; }


        public OdisDataComparer(OdisDataComparerSettings settings = null)
        {
            Settings = settings ?? new OdisDataComparerSettings();
        }


        public static ICollection<String> EmptyStringList = new List<String> { String.Empty };

        public ComparisonResults CompareEcus(Dictionary<String, Ecu> firstSet, Dictionary<String, Ecu> secondSet)
        {
            var result = new ComparisonResults();

            result.EcusMissingInFirst = secondSet.Where(kvp => secondSet.Keys.Except(firstSet.Keys).Contains(kvp.Key)).ToDictionary();
            result.EcusMissingInSecond = firstSet.Where(kvp => firstSet.Keys.Except(secondSet.Keys).Contains(kvp.Key)).ToDictionary();

            foreach (var ecuId in firstSet.Keys.Intersect(secondSet.Keys))
            {
                var firstEcu = firstSet[ecuId];
                var secondEcu = secondSet[ecuId];

                var ecuComparisonResult = CompareEcu(firstEcu, secondEcu);
                if (!ecuComparisonResult.IsEmpty)
                    result.EcusComparisonResult.Add(ecuComparisonResult);
            }

            return result;
        }


        public EcuComparisonResult CompareEcu(Ecu first, Ecu second)
        {
            if (first.EcuId != second.EcuId)
                throw new InvalidDataException($"ECU_ID: {first.EcuId} is different from {second.EcuId}!");

            var ecuComparisonResult = new EcuComparisonResult();

            ecuComparisonResult.EcuId = first.EcuId;
            ecuComparisonResult.First = first;
            ecuComparisonResult.Second = second;

            // COMPARE MASTER DATA
            ecuComparisonResult.MasterEcuDataMissingInFirst = second.EcuMasters.FilterByPredicate(kvp => second.EcuMasters.Dictionary.Keys.Except(first.EcuMasters.Dictionary.Keys ?? EmptyStringList).Contains(kvp.Key), MeaningfulText.Map);
            ecuComparisonResult.MasterEcuDataMissingInSecond = first.EcuMasters.FilterByPredicate(kvp => first.EcuMasters.Dictionary.Keys.Except(second.EcuMasters.Dictionary.Keys ?? EmptyStringList).Contains(kvp.Key), MeaningfulText.Map);

            // for the master ecu (it is just one) the keys here ident, adaption_read, coding_read
            foreach (var ecuMasterType in first.EcuMasters.Dictionary.Keys.Intersect(second.EcuMasters.Dictionary.Keys))
            {
                var firstData = first.EcuMasters.Dictionary[ecuMasterType];
                var secondData = second.EcuMasters.Dictionary[ecuMasterType];

                var compareEcuDataResult = CompareEcuData(new List<String> { first.EcuId, MeaningfulText.Map(ecuMasterType) }, firstData, secondData);
                if (!compareEcuDataResult.IsEmpty)
                    ecuComparisonResult.MasterDataComparisonResult.Add(compareEcuDataResult);
            }

            // COMPARE SUBSYSTEM DATA
            ecuComparisonResult.SubsystemEcuDataMissingInFirst = second.EcuSubsystems?.Subsystems?.FilterByPredicate(kvp => second.EcuSubsystems?.Subsystems?.Dictionary?.Keys?.Except(first.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList)?.Contains(kvp.Key) ?? false, MeaningfulText.Map);
            ecuComparisonResult.SubsystemEcuDataMissingInSecond = first.EcuSubsystems?.Subsystems?.FilterByPredicate(kvp => first.EcuSubsystems?.Subsystems?.Dictionary?.Keys?.Except(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList)?.Contains(kvp.Key) ?? false, MeaningfulText.Map);

            // for the subsystem ecus (they can be more than one) the keys here ident_<display_value of the value with ti_name MAS01171 (subsystem number)>, adaption_read_<ti_name> or coding_read_<ti_name>
            var commonSubsystems = first.EcuSubsystems?.Subsystems?.Dictionary?.Keys.Intersect(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys);
            if (commonSubsystems != null)
            {
                foreach (var ecuSubsystemType in commonSubsystems)
                {
                    var firstData = first.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];
                    var secondData = second.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];

                    var compareEcuDataResult = CompareEcuData(new List<String> { first.EcuId, "subsystem", MeaningfulText.Map(ecuSubsystemType) }, firstData, secondData);
                    if (!compareEcuDataResult.IsEmpty)
                        ecuComparisonResult.SubsystemDataComparisonResult.Add(compareEcuDataResult);
                }
            }

            return ecuComparisonResult;
        }



        public EcuDataComparisonResult CompareEcuData(List<String> mainpath, EcuData first, EcuData second)
        {
            var result = new EcuDataComparisonResult { First = first, Second = second };

            if (first.TiName != second.TiName)
                throw new InvalidDataException($"TI_NAME: {first.TiName} is different from {second.TiName}!");

            CompareStrings(result.Messages, new List<String>(mainpath) { first.TiName }, new List<String>(mainpath) { first.DisplayName, second.DisplayName }, FieldPropertyEnum.DisplayName, first.DisplayName, second.DisplayName);

            // Compare values
            result.FieldsMissingInFirst = second.Values?.FilterByPredicate(kvp => second.Values?.Dictionary?.Keys.Except(first.Values?.Dictionary?.Keys ?? EmptyStringList).Contains(kvp.Key) ?? false);
            result.FieldsMissingInSecond = first.Values?.FilterByPredicate(kvp => first.Values?.Dictionary?.Keys.Except(second.Values?.Dictionary?.Keys ?? EmptyStringList).Contains(kvp.Key) ?? false);

            // keys here are actual values
            foreach (var valueItemKey in first.Values.Dictionary.Keys.Intersect(second.Values.Dictionary.Keys))
            {
                var firstValue = first.Values.Dictionary[valueItemKey];
                var secondValue = second.Values.Dictionary[valueItemKey];

                var path = new List<String>(mainpath) { first.TiName, valueItemKey };

                var fieldDescriptions = new List<String>() { firstValue.DisplayName, secondValue.DisplayName }.Distinct().ToList();

                CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.TiName, firstValue.TiName, secondValue.TiName);
                CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.TiUnit, firstValue.TiUnit, secondValue.TiUnit);
                CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.DisplayName, firstValue.DisplayName, secondValue.DisplayName);
                CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.DisplayValue, firstValue.DisplayValue, secondValue.DisplayValue);
                CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.DisplayUnit, firstValue.DisplayUnit, secondValue.DisplayUnit);
                if (!CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.BinValue, firstValue.BinValue, secondValue.BinValue))
                    if (!CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.HexValue, firstValue.HexValue, secondValue.HexValue))
                        CompareStrings(result.Messages, path, fieldDescriptions, FieldPropertyEnum.TiValue, firstValue.TiValue, secondValue.TiValue);
            }


            // TODO SUBVALUES

            return result;
        }



        public bool CompareStrings(List<DifferenceMessage> errors, List<String> path, List<String> fieldDescriptions, FieldPropertyEnum fieldProperty, String first, String second)
        {
            if (String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second))
                return false;

            if (!Settings.FieldToBypassOnComparison.Contains(fieldProperty) && (
                (String.IsNullOrWhiteSpace(first) && !String.IsNullOrWhiteSpace(second)) ||
                (!String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second)) ||
                (first != second)))
            {
                errors.Add(new DifferenceMessage { Path = path, FieldDescriptions = fieldDescriptions, FieldProperty = fieldProperty, Message = $"{first} is different from {second}", ValueFirst = first, ValueSecond = second });
                return true;
            }

            return false;
        }
    }
}
