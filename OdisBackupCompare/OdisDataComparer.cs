using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OdisBackupCompare
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FieldType
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
        public List<FieldType> FieldToBypassOnComparison { get; init; }

        public OdisDataComparerSettings()
        {
            FieldToBypassOnComparison = new List<FieldType>();
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

            result.EcusMissingInFirst = secondSet.Keys.Except(firstSet.Keys).ToList();
            result.EcusMissingInSecond = firstSet.Keys.Except(secondSet.Keys).ToList();

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
            ecuComparisonResult.MasterDataMissingInFirst = second.EcuMasters.Dictionary.Keys.Except(first.EcuMasters.Dictionary.Keys ?? EmptyStringList).ToList();
            ecuComparisonResult.MasterDataMissingInSecond = first.EcuMasters.Dictionary.Keys.Except(second.EcuMasters.Dictionary.Keys ?? EmptyStringList).ToList();

            // for the master ecu (it is just one) the keys here ident, adaption_read, coding_read
            foreach (var ecuMasterType in first.EcuMasters.Dictionary.Keys.Intersect(second.EcuMasters.Dictionary.Keys))
            {
                var firstData = first.EcuMasters.Dictionary[ecuMasterType];
                var secondData = second.EcuMasters.Dictionary[ecuMasterType];

                var compareEcuDataResult = CompareEcuData($"{first.EcuId}_{ecuMasterType}", firstData, secondData);
                if (!compareEcuDataResult.IsEmpty)
                    ecuComparisonResult.MasterDataComparisonResult.Add(compareEcuDataResult);
            }

            // COMPARE SUBSYSTEM DATA
            ecuComparisonResult.SubsystemDataMissingInFirst = second.EcuSubsystems?.Subsystems?.Dictionary?.Keys?.Except(first.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList).ToList();
            ecuComparisonResult.SubsystemDataMissingInSecond = first.EcuSubsystems?.Subsystems?.Dictionary?.Keys.Except(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys ?? EmptyStringList).ToList();

            // for the subsystem ecus (they can be more than one) the keys here ident_<display_value of the value with ti_name MAS01171 (subsystem number)>, adaption_read_<ti_name> or coding_read_<ti_name>
            var commonSubsystems = first.EcuSubsystems?.Subsystems?.Dictionary?.Keys.Intersect(second.EcuSubsystems?.Subsystems?.Dictionary?.Keys);
            if (commonSubsystems != null)
            {
                foreach (var ecuSubsystemType in commonSubsystems)
                {
                    var firstData = first.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];
                    var secondData = second.EcuSubsystems.Subsystems.Dictionary[ecuSubsystemType];

                    var compareEcuDataResult = CompareEcuData($"{first.EcuId}_{ecuSubsystemType}", firstData, secondData);
                    if (!compareEcuDataResult.IsEmpty)
                        ecuComparisonResult.SubsystemDataComparisonResult.Add(compareEcuDataResult);
                }
            }

            return ecuComparisonResult;
        }



        public EcuDataComparisonResult CompareEcuData(String ecuMasterType, EcuData first, EcuData second)
        {
            var result = new EcuDataComparisonResult { First = first, Second = second };

            if (first.TiName != second.TiName)
                throw new InvalidDataException($"TI_NAME: {first.TiName} is different from {second.TiName}!");

            var mainPath = $"{ecuMasterType}:{first.TiName}";
           
            CompareStrings(result.Messages, mainPath, FieldType.DisplayName, first.DisplayName, second.DisplayName);

            // Compare values
            result.FieldsMissingInFirst = second.Values?.Dictionary?.Keys.Except(first.Values?.Dictionary?.Keys ?? EmptyStringList).ToList();
            result.FieldsMissingInSecond = first.Values?.Dictionary?.Keys.Except(second.Values?.Dictionary?.Keys ?? EmptyStringList).ToList();

            // keys here are actual values
            foreach (var valueItemKey in first.Values.Dictionary.Keys.Intersect(second.Values.Dictionary.Keys))
            {
                var path = $"{mainPath}:{valueItemKey}";

                var firstValue = first.Values.Dictionary[valueItemKey];
                var secondValue = second.Values.Dictionary[valueItemKey];

                CompareStrings(result.Messages, path, FieldType.TiName, firstValue.TiName, secondValue.TiName);
                CompareStrings(result.Messages, path, FieldType.TiUnit, firstValue.TiUnit, secondValue.TiUnit);
                CompareStrings(result.Messages, path, FieldType.DisplayName, firstValue.DisplayName, secondValue.DisplayName);
                CompareStrings(result.Messages, path, FieldType.DisplayValue, firstValue.DisplayValue, secondValue.DisplayValue);
                CompareStrings(result.Messages, path, FieldType.DisplayUnit, firstValue.DisplayUnit, secondValue.DisplayUnit);
                if (!CompareStrings(result.Messages, path, FieldType.BinValue, firstValue.BinValue, secondValue.BinValue))
                    if (!CompareStrings(result.Messages, path, FieldType.HexValue, firstValue.HexValue, secondValue.HexValue))
                        CompareStrings(result.Messages, path, FieldType.TiValue, firstValue.TiValue, secondValue.TiValue);
            }


            // TODO SUBVALUES

            return result;
        }



        public bool CompareStrings(List<DifferenceMessage> errors, String key, FieldType fieldType, String first, String second)
        {
            if (String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second))
                return false;

            if (!Settings.FieldToBypassOnComparison.Contains(fieldType) && (
                (String.IsNullOrWhiteSpace(first) && !String.IsNullOrWhiteSpace(second)) ||
                (!String.IsNullOrWhiteSpace(first) && String.IsNullOrWhiteSpace(second)) ||
                (first.Trim() != second.Trim())))
            {
                errors.Add(new DifferenceMessage { Path = key, FieldName = JsonSerializer.Serialize(fieldType), Message = $"{first} is different from {second}" });
                return true;
            }

            return false;
        }
    }
}
