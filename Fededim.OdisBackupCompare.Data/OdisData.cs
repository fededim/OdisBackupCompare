using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Fededim.OdisBackupCompare.Data
{
    public class DictionaryListTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type.Name.StartsWith("DictionaryList"))
                type = type.BaseType;

            return base.GetTypeInfo(type, options);
        }

    }



    public class DictionaryList<T, TKey> : List<T>
    {
        protected Dictionary<TKey, T> _dictionaryLookup { get; set; }
        protected Func<T, int, TKey> ProjectionFunction { get; set; }

        public DictionaryList(Func<T, int, TKey> projectionFunction) : base()
        {
            ProjectionFunction = projectionFunction;
        }


        [JsonIgnore]
        public Dictionary<TKey, T> Dictionary
        {
            get
            {
                if (_dictionaryLookup == null)
                {
                    _dictionaryLookup = new Dictionary<TKey, T>();
                    foreach (var el in this)
                    {
                        // to force an unique key since in the gateways there are repeated values for the same ti_name (e.g. SFT0004B)
                        TKey key;
                        var i = 1;
                        do
                        {
                            key = ProjectionFunction(el, i++);
                        }
                        while (_dictionaryLookup.ContainsKey(key));

                        _dictionaryLookup.Add(key, el);
                    }
                }

                return _dictionaryLookup;
            }
        }


        public Dictionary<TKey, T> FilterByPredicate(Predicate<KeyValuePair<TKey, T>> predicate, Func<TKey, TKey> keyBeautify = null)
        {
            var result = Dictionary.Where(kvp => predicate(kvp));

            if (keyBeautify != null)
                result = result.Select(kvp => new KeyValuePair<TKey, T>(keyBeautify(kvp.Key), kvp.Value));

            return result.ToDictionary();
        }

    }


    [XmlRoot("protocol")]
    [XmlInclude(typeof(Information))]
    [XmlInclude(typeof(Vehicle))]
    [Serializable]
    public class OdisData
    {
        [XmlElement("time_of_issue")]
        public String TimeOfIssue { get; set; }

        [XmlElement("charset")]
        public String Charset { get; set; }

        [XmlElement("orientation_right_to_left")]
        public bool OrientationRightToLeft { get; set; }

        [XmlElement("information")]
        public Information Information { get; set; }

        [XmlElement("vehicle")]
        public Vehicle Vehicle { get; set; }



        public static OdisData ParseFromXml(String xmlContent)
        {
            var serializer = new XmlSerializer(typeof(OdisData));
            using var reader = new StringReader(xmlContent);
            return serializer.Deserialize(reader) as OdisData;
        }


        public static OdisData ParseFromFile(String filePath)
        {
            var serializer = new XmlSerializer(typeof(OdisData));
            using var reader = new StreamReader(filePath);
            return serializer.Deserialize(reader) as OdisData;
        }


        public String GetVin()
        {
            return Vehicle?.VehicleData?.FirstOrDefault(v => v.DisplayName == "vin")?.DisplayValue;
        }

        public String GetMileage()
        {
            return Vehicle?.VehicleData?.FirstOrDefault(v => v.DisplayName == "mileage")?.DisplayValue;
        }

        public Dictionary<String, Ecu> GetEcus(String ecuId = null, String ecuName = null)
        {
            var ecus = new List<Ecu>();

            if (Vehicle?.Communications != null)
            {
                foreach (var comm in Vehicle.Communications)
                {
                    ecus.AddRange(comm.CommunicationEcus.SelectMany(ce => ce?.Ecus?.Where(e =>
                    (String.IsNullOrEmpty(ecuId) || e.EcuId.Contains(ecuId)) &&
                    (String.IsNullOrEmpty(ecuName) || e.EcuName.Contains(ecuName))
                    )));
                }
            }

            return ecus.ToDictionary(ecu => ecu.EcuId);
        }
    }


    public static class MeaningfulText
    {
        public static Dictionary<string, string> RemapData = new Dictionary<string, string> {
            { "ident","Identification" },
            { "coding_read","Coding" },
            { "adaption_read","Adaptation" },
        };


        public static string Map(string s)
        {
            return Regex.Replace(s, $"({String.Join('|', RemapData.Keys)})", m => RemapData[m.Value]);
        }
    }



    [Serializable]
    public class Information
    {
        [XmlElement("document_name")]
        public String DocumentName { get; set; }

        [XmlElement("software_name")]
        public String SoftwareName { get; set; }

        [XmlElement("login_account")]
        public String LoginAccount { get; set; }

        [XmlElement("pc_name")]
        public String PcName { get; set; }

        [XmlElement("pc_os")]
        public String PcOs { get; set; }

        [XmlElement("pc_ecu")]
        public String PcEcu { get; set; }

        [XmlElement("pc_hardware_id")]
        public String PcHardwareId { get; set; }

        [XmlElement("pc_ram")]
        public long PcRam { get; set; }

        [XmlElement("diag_hardware")]
        public String DiagHardware { get; set; }

        [XmlElement("workshop_code")]
        public WorkshopCode WorkshopCode { get; set; }

        [XmlElement("version")]
        public VersionInfo Version { get; set; }

        [XmlElement("confidentiality_level")]
        public String ConfidentialityLevel { get; set; }
    }

    [Serializable]
    public class WorkshopCode
    {
        [XmlElement("serial_number")]
        public String SerialNumber { get; set; }

        [XmlElement("importer_number")]
        public String ImporterNumber { get; set; }

        [XmlElement("dealer_number")]
        public String DealerNumber { get; set; }

        [XmlElement("fingerprint")]
        public String Fingerprint { get; set; }
    }

    [Serializable]
    public class VersionInfo
    {
        [XmlElement("number")]
        public String Number { get; set; }

        [XmlElement("showBeta")]
        public bool ShowBeta { get; set; }

        [XmlElement("release")]
        public String Release { get; set; }

        [XmlElement("kernel")]
        public String Kernel { get; set; }

        [XmlElement("mcd")]
        public String Mcd { get; set; }

        [XmlElement("ecf")]
        public String Ecf { get; set; }

        [XmlElement("pdu_api")]
        public String PduApi { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(NameValueData))]
    [XmlInclude(typeof(Communication))]

    public class Vehicle
    {
        [XmlElement("vehicle_data")]
        public DictionaryList<NameValueData, String> VehicleData { get; set; } = new DictionaryList<NameValueData, String>((el, uniqueIndex) => el.DisplayName);

        [XmlElement("odx_info")]
        public DictionaryList<NameValueData, String> OdxInfo { get; set; } = new DictionaryList<NameValueData, String>((el, uniqueIndex) => el.DisplayName);

        [XmlElement("communications")]
        public List<Communication> Communications { get; set; }
    }



    [Serializable]
    public class NameValueData
    {
        [XmlElement("display_name")]
        public String DisplayName { get; set; }

        [XmlElement("display_value")]
        public String DisplayValue { get; set; }
    }



    [Serializable]
    [XmlInclude(typeof(CommunicationEcu))]
    public class Communication
    {
        [XmlElement("ecus")]
        public List<CommunicationEcu> CommunicationEcus { get; set; }

        [XmlAttribute("type")]
        public String Type { get; set; }
    }


    [Serializable]
    [XmlInclude(typeof(Ecu))]
    public class CommunicationEcu
    {
        [XmlElement("ecu")]
        public List<Ecu> Ecus { get; set; }

        [XmlAttribute("type")]
        public String Type { get; set; }
    }



    [Serializable]
    [XmlInclude(typeof(EcuData))]
    [XmlInclude(typeof(EcuSubsystems))]
    public class Ecu
    {
        [XmlElement("time_stamp")]
        public DateTime TimeStamp { get; set; }

        [XmlElement("ecu_id")]
        public String EcuId { get; set; }

        [XmlElement("ecu_name")]
        public String EcuName { get; set; }

        [XmlElement("logicallink")]
        public String LogicalLink { get; set; }

        [XmlElement("tester_odx_variant")]
        public String TesterOdxVariant { get; set; }

        [XmlElement("ecu_master")]
        [JsonIgnore]
        public DictionaryList<EcuData, String> EcuMasters { get; set; } = new DictionaryList<EcuData, String>((el, uniqueIndex) => el.Type);

        [XmlElement("ecu_subsystem")]
        [JsonIgnore]
        public EcuSubsystems EcuSubsystems { get; set; }


        public List<EcuData> GetEcuIdentification()
        {
            return EcuMasters.Where(m => m.Type == "ident").ToList();
        }


        public List<EcuData> GetEcuCoding()
        {
            return EcuMasters.Where(m => m.Type == "coding_read").ToList();
        }

        public List<EcuData> GetEcuAdaptation()
        {
            return EcuMasters.Where(m => m.Type == "adaption_read").ToList();
        }


        public override string ToString()
        {
            return $"{EcuId} {EcuName} ({LogicalLink} {TesterOdxVariant})";
        }
    }




    [Serializable]
    [XmlInclude(typeof(ValueItem))]
    [XmlInclude(typeof(SwapFodStatus))]
    public class EcuData
    {
        [XmlAttribute("type")]
        public String Type { get; set; }

        [XmlElement("time_stamp")]
        public DateTime TimeStamp { get; set; }

        [XmlElement("display_name")]
        public String DisplayName { get; set; }

        [XmlElement("ti_name")]
        public String TiName { get; set; }

        [XmlElement("values")]
        public DictionaryList<ValueItem, String> Values { get; set; } = new DictionaryList<ValueItem, String>((el, uniqueIndex) => el.TiName ?? el.DisplayName);

        [XmlElement("swap_fod_status")]
        public SwapFodStatus SwapFodStatus { get; set; }

        public override string ToString()
        {
            return $"{MeaningfulText.Map(Type)}: {TiName}";
        }
    }


    [Serializable]
    [XmlInclude(typeof(ValueItem))]
    public class ValueItem
    {
        [XmlElement("ti_name")]
        public String TiName { get; set; }

        [XmlElement("display_name")]
        public String DisplayName { get; set; }

        [XmlElement("display_value")]
        public String DisplayValue { get; set; }

        [XmlElement("bin_value")]
        public String BinValue { get; set; }

        [XmlElement("hex_value")]
        public String HexValue { get; set; }

        [XmlElement("ti_value")]
        public String TiValue { get; set; }

        [XmlElement("ti_unit")]
        public String TiUnit { get; set; }

        [XmlElement("display_unit")]
        public String DisplayUnit { get; set; }

        [XmlElement("values")]
        public DictionaryList<ValueItem, String> SubValues { get; set; } = new DictionaryList<ValueItem, String>((el, uniqueIndex) => (uniqueIndex > 1) ? $"{el.TiName ?? el.DisplayName}_{uniqueIndex}" : $"{el.TiName ?? el.DisplayName}");

        public String GetName()
        {
            if (!String.IsNullOrWhiteSpace(DisplayName))
            {
                if (!String.IsNullOrWhiteSpace(TiName) && TiName != DisplayName)
                    return $"{DisplayName} ({TiName})";
                else
                    return DisplayName;
            }
            else
                return TiName;
        }

        public String GetValue()
        {
            StringBuilder sb = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(BinValue))
                sb.Append(BinValue);
            else if (!String.IsNullOrWhiteSpace(HexValue))
                sb.Append(HexValue);
            else if (!String.IsNullOrWhiteSpace(DisplayValue))
                sb.Append(DisplayValue);
            else
                sb.Append(TiValue);

            if (!String.IsNullOrWhiteSpace(DisplayUnit))
                sb.Append($" ({DisplayUnit})");
            else if (!String.IsNullOrWhiteSpace(TiUnit))
                sb.Append($" ({TiUnit})");

            return sb.ToString();
        }



        public FieldParametersEnum FieldParameters(FieldPropertyEnum fieldProperty)
        {
            FieldParametersEnum result = 0;

            if (String.IsNullOrWhiteSpace(TiValue))
                result |= FieldParametersEnum.IsFreeText;

            if (!String.IsNullOrWhiteSpace(TiUnit) && fieldProperty == FieldPropertyEnum.DisplayValue)
                result |= FieldParametersEnum.IsNumerical;

            return result;
        }
    }



    [Serializable]
    [XmlInclude(typeof(SwapStateFunctionsUds))]
    public class SwapFodStatus
    {
        [XmlElement("swap_public_key")]
        public String SwapPublicKey { get; set; }

        [XmlElement("swap_state_functions_uds")]
        public List<SwapStateFunctionsUds> SwapStateFunctionsUds { get; set; }
    }

    [Serializable]
    public class SwapStateFunctionsUds
    {
        [XmlElement("swap_state")]
        public String SwapState { get; set; }

        [XmlElement("swap_state_function")]
        public List<SwapStateFunction> SwapStateFunctions { get; set; }
    }

    [Serializable]
    public class SwapStateFunction
    {
        [XmlElement("function_sid")]
        public String FunctionSid { get; set; }

        [XmlElement("status_byte")]
        public String StatusByte { get; set; }
    }


    [Serializable]
    [XmlInclude(typeof(EcuData))]
    public class EcuSubsystems
    {
        [XmlElement("subsystem")]
        public DictionaryList<EcuData, String> Subsystems { get; set; } = new DictionaryList<EcuData, String>((el, uniqueIndex) => $"{el.TiName ?? el.Values.First(v => v.TiName == "MAS01171").DisplayValue}_{el.Type}");
    }



}