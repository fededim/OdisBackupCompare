using System.Net.WebSockets;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace OdisBackupCompare
{
    public class DictionaryList<T, TKey> : List<T>
    {
        protected Dictionary<TKey, T> _dictionaryLookup { get; set; }
        protected Func<T, TKey> ProjectionFunction { get; set; }

        public DictionaryList(Func<T, TKey> projectionFunction) : base()
        {
            ProjectionFunction = projectionFunction;
        }

        [JsonIgnore]
        public Dictionary<TKey, T> Dictionary
        {
            get
            {
                if (_dictionaryLookup == null)
                    _dictionaryLookup = this.ToDictionary(ProjectionFunction);

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
        static Dictionary<string, string> RemapData = new Dictionary<string, string> {
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
        public DictionaryList<NameValueData, String> VehicleData { get; set; } = new DictionaryList<NameValueData, String>(el => el.DisplayName);

        [XmlElement("odx_info")]
        public DictionaryList<NameValueData, String> OdxInfo { get; set; } = new DictionaryList<NameValueData, String>(el => el.DisplayName);

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
        public DictionaryList<EcuData, String> EcuMasters { get; set; } = new DictionaryList<EcuData, String>((el) => el.Type);

        [XmlElement("ecu_subsystem")]
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
        public DictionaryList<ValueItem, String> Values { get; set; } = new DictionaryList<ValueItem, String>((el) => el.TiName ?? el.DisplayName);

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
        public DictionaryList<ValueItem, String> SubValues { get; set; } = new DictionaryList<ValueItem, String>((el) => el.TiName ?? el.DisplayName);
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
        public DictionaryList<EcuData, String> Subsystems { get; set; } = new DictionaryList<EcuData, String>((el) => $"{el.Type}_{el.TiName ?? el.Values.First(v => v.TiName == "MAS01171").DisplayValue}");
    }



    public class ComparisonResults
    {
        public Dictionary<String, Ecu> EcusMissingInFirst { get; set; }
        public Dictionary<String, Ecu> EcusMissingInSecond { get; set; }

        public DictionaryList<EcuComparisonResult, String> EcusComparisonResult { get; set; }


        public ComparisonResults()
        {
            EcusComparisonResult = new DictionaryList<EcuComparisonResult, String>(ecu => ecu.EcuId);
            EcusMissingInFirst = new Dictionary<String, Ecu>();
            EcusMissingInSecond = new Dictionary<String, Ecu>();
        }
    }



    public class EcuComparisonResult
    {
        public String EcuId { get; set; }
        public String[] EcuNames => new String[] { First.EcuName, Second.EcuName }.Distinct().ToArray();

        [JsonIgnore]
        public Ecu First { get; set; }
        [JsonIgnore]
        public Ecu Second { get; set; }

        // MASTER DATA
        public Dictionary<String, EcuData> MasterEcuDataMissingInFirst { get; set; }
        public Dictionary<String, EcuData> MasterEcuDataMissingInSecond { get; set; }


        public List<EcuDataComparisonResult> MasterDataComparisonResult { get; set; }


        // SUBSYSTEM DATA
        public Dictionary<String, EcuData> SubsystemEcuDataMissingInFirst { get; set; }
        public Dictionary<String, EcuData> SubsystemEcuDataMissingInSecond { get; set; }

        public List<EcuDataComparisonResult> SubsystemDataComparisonResult { get; set; }


        public EcuComparisonResult()
        {
            MasterDataComparisonResult = new List<EcuDataComparisonResult>();
            MasterEcuDataMissingInFirst = new Dictionary<String, EcuData>();
            MasterEcuDataMissingInSecond = new Dictionary<String, EcuData>();

            SubsystemDataComparisonResult = new List<EcuDataComparisonResult>();
            SubsystemEcuDataMissingInFirst = new Dictionary<String, EcuData>();
            SubsystemEcuDataMissingInSecond = new Dictionary<String, EcuData>();
        }

        [JsonIgnore]
        public bool IsEmpty => MasterEcuDataMissingInFirst.Count == 0 && MasterEcuDataMissingInSecond.Count == 0 && MasterDataComparisonResult.Count == 0 &&
            SubsystemEcuDataMissingInFirst?.Count == 0 && SubsystemEcuDataMissingInSecond?.Count == 0 && SubsystemDataComparisonResult?.Count == 0;
    }



    public class EcuDataComparisonResult
    {
        public Dictionary<String, ValueItem> FieldsMissingInFirst { get; set; }
        public Dictionary<String, ValueItem> FieldsMissingInSecond { get; set; }

        public List<DifferenceMessage> Messages { get; set; }

        [JsonIgnore]
        public EcuData First { get; set; }
        [JsonIgnore]
        public EcuData Second { get; set; }


        public EcuDataComparisonResult()
        {
            Messages = new List<DifferenceMessage>();

            FieldsMissingInFirst = new Dictionary<String, ValueItem>();
            FieldsMissingInSecond = new Dictionary<String, ValueItem>();
        }

        [JsonIgnore]
        public bool IsEmpty => FieldsMissingInFirst.Count == 0 && FieldsMissingInSecond.Count == 0 && Messages.Count == 0;
    }



    public class DifferenceMessage
    {
        public List<String> Path { get; set; }
        public List<String> FieldDescriptions { get; set; }
        public FieldPropertyEnum FieldProperty { get; set; }
        public String Message { get; set; }
        public String ValueFirst { get; set; }
        public String ValueSecond { get; set; }

        public DifferenceMessage()
        {
            Path = new List<String>();
        }
    }
}