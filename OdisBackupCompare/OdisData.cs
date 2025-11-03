using System.Net.WebSockets;
using System.Text.Json.Serialization;
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

        public Dictionary<TKey, T> Dictionary
        {
            get
            {
                if (_dictionaryLookup == null)
                    _dictionaryLookup = this.ToDictionary(ProjectionFunction);

                return _dictionaryLookup;
            }
        }
    }


    [XmlRoot("protocol")]
    [XmlInclude(typeof(Information))]
    [XmlInclude(typeof(Vehicle))]
    [Serializable]
    public class OdisData
    {
        [XmlElement("time_of_issue")]
        public string TimeOfIssue { get; set; }

        [XmlElement("charset")]
        public string Charset { get; set; }

        [XmlElement("orientation_right_to_left")]
        public bool OrientationRightToLeft { get; set; }

        [XmlElement("information")]
        public Information Information { get; set; }

        [XmlElement("vehicle")]
        public Vehicle Vehicle { get; set; }



        public static OdisData ParseFromXml(string xmlContent)
        {
            var serializer = new XmlSerializer(typeof(OdisData));
            using var reader = new StringReader(xmlContent);
            return serializer.Deserialize(reader) as OdisData;
        }


        public static OdisData ParseFromFile(string filePath)
        {
            var serializer = new XmlSerializer(typeof(OdisData));
            using var reader = new StreamReader(filePath);
            return serializer.Deserialize(reader) as OdisData;
        }


        public string GetVin()
        {
            return Vehicle?.VehicleData?.FirstOrDefault(v => v.DisplayName == "vin")?.DisplayValue;
        }

        public string GetMileage()
        {
            return Vehicle?.VehicleData?.FirstOrDefault(v => v.DisplayName == "mileage")?.DisplayValue;
        }

        public Dictionary<String, Ecu> GetEcus(string ecuId = null, string ecuName = null)
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

    [Serializable]
    public class Information
    {
        [XmlElement("document_name")]
        public string DocumentName { get; set; }

        [XmlElement("software_name")]
        public string SoftwareName { get; set; }

        [XmlElement("login_account")]
        public string LoginAccount { get; set; }

        [XmlElement("pc_name")]
        public string PcName { get; set; }

        [XmlElement("pc_os")]
        public string PcOs { get; set; }

        [XmlElement("pc_ecu")]
        public string PcEcu { get; set; }

        [XmlElement("pc_hardware_id")]
        public string PcHardwareId { get; set; }

        [XmlElement("pc_ram")]
        public long PcRam { get; set; }

        [XmlElement("diag_hardware")]
        public string DiagHardware { get; set; }

        [XmlElement("workshop_code")]
        public WorkshopCode WorkshopCode { get; set; }

        [XmlElement("version")]
        public VersionInfo Version { get; set; }

        [XmlElement("confidentiality_level")]
        public string ConfidentialityLevel { get; set; }
    }

    [Serializable]
    public class WorkshopCode
    {
        [XmlElement("serial_number")]
        public string SerialNumber { get; set; }

        [XmlElement("importer_number")]
        public string ImporterNumber { get; set; }

        [XmlElement("dealer_number")]
        public string DealerNumber { get; set; }

        [XmlElement("fingerprint")]
        public string Fingerprint { get; set; }
    }

    [Serializable]
    public class VersionInfo
    {
        [XmlElement("number")]
        public string Number { get; set; }

        [XmlElement("showBeta")]
        public bool ShowBeta { get; set; }

        [XmlElement("release")]
        public string Release { get; set; }

        [XmlElement("kernel")]
        public string Kernel { get; set; }

        [XmlElement("mcd")]
        public string Mcd { get; set; }

        [XmlElement("ecf")]
        public string Ecf { get; set; }

        [XmlElement("pdu_api")]
        public string PduApi { get; set; }
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
        public string DisplayName { get; set; }

        [XmlElement("display_value")]
        public string DisplayValue { get; set; }
    }



    [Serializable]
    [XmlInclude(typeof(CommunicationEcu))]
    public class Communication
    {
        [XmlElement("ecus")]
        public List<CommunicationEcu> CommunicationEcus { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }


    [Serializable]
    [XmlInclude(typeof(Ecu))]
    public class CommunicationEcu
    {
        [XmlElement("ecu")]
        public List<Ecu> Ecus { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; }
    }



    [Serializable]
    [XmlInclude(typeof(EcuData))]
    [XmlInclude(typeof(EcuSubsystems))]
    public class Ecu
    {
        [XmlElement("time_stamp")]
        public DateTime TimeStamp { get; set; }

        [XmlElement("ecu_id")]
        public string EcuId { get; set; }

        [XmlElement("ecu_name")]
        public string EcuName { get; set; }

        [XmlElement("logicallink")]
        public string LogicalLink { get; set; }

        [XmlElement("tester_odx_variant")]
        public string TesterOdxVariant { get; set; }

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
    }




    [Serializable]
    [XmlInclude(typeof(ValueItem))]
    [XmlInclude(typeof(SwapFodStatus))]
    public class EcuData
    {
        [XmlAttribute("type")]
        public string Type { get; set; }

        [XmlElement("time_stamp")]
        public DateTime TimeStamp { get; set; }

        [XmlElement("display_name")]
        public string DisplayName { get; set; }

        [XmlElement("ti_name")]
        public string TiName { get; set; }

        [XmlElement("values")]
        public DictionaryList<ValueItem, String> Values { get; set; } = new DictionaryList<ValueItem, String>((el) => el.TiName ?? el.DisplayName);

        [XmlElement("swap_fod_status")]
        public SwapFodStatus SwapFodStatus { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(ValueItem))]
    public class ValueItem
    {
        [XmlElement("ti_name")]
        public string TiName { get; set; }

        [XmlElement("display_name")]
        public string DisplayName { get; set; }

        [XmlElement("display_value")]
        public string DisplayValue { get; set; }

        [XmlElement("bin_value")]
        public string BinValue { get; set; }

        [XmlElement("hex_value")]
        public string HexValue { get; set; }

        [XmlElement("ti_value")]
        public string TiValue { get; set; }

        [XmlElement("ti_unit")]
        public string TiUnit { get; set; }

        [XmlElement("display_unit")]
        public string DisplayUnit { get; set; }

        [XmlElement("values")]
        public DictionaryList<ValueItem, String> SubValues { get; set; } = new DictionaryList<ValueItem, String>((el) => el.TiName ?? el.DisplayName);
    }

    [Serializable]
    [XmlInclude(typeof(SwapStateFunctionsUds))]
    public class SwapFodStatus
    {
        [XmlElement("swap_public_key")]
        public string SwapPublicKey { get; set; }

        [XmlElement("swap_state_functions_uds")]
        public List<SwapStateFunctionsUds> SwapStateFunctionsUds { get; set; }
    }

    [Serializable]
    public class SwapStateFunctionsUds
    {
        [XmlElement("swap_state")]
        public string SwapState { get; set; }

        [XmlElement("swap_state_function")]
        public List<SwapStateFunction> SwapStateFunctions { get; set; }
    }

    [Serializable]
    public class SwapStateFunction
    {
        [XmlElement("function_sid")]
        public string FunctionSid { get; set; }

        [XmlElement("status_byte")]
        public string StatusByte { get; set; }
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
        public List<String> EcusMissingInFirst { get; set; }
        public List<String> EcusMissingInSecond { get; set; }

        public DictionaryList<EcuComparisonResult, String> EcusComparisonResult { get; set; }


        public ComparisonResults()
        {
            EcusComparisonResult = new DictionaryList<EcuComparisonResult, String>(ecu => ecu.EcuId);
        }
    }



    public class EcuComparisonResult
    {
        public String EcuId { get; set; }

        public Ecu First { get; set; }
        public Ecu Second { get; set; }

        // MASTER DATA
        public List<String> MasterDataMissingInFirst { get; set; }
        public List<String> MasterDataMissingInSecond { get; set; }
        public List<EcuDataComparisonResult> MasterDataComparisonResult { get; set; }


        // SUBSYSTEM DATA
        public List<String> SubsystemDataMissingInFirst { get; set; }
        public List<String> SubsystemDataMissingInSecond { get; set; }
        public List<EcuDataComparisonResult> SubsystemDataComparisonResult { get; set; }


        public EcuComparisonResult()
        {
            MasterDataComparisonResult = new List<EcuDataComparisonResult>();
            SubsystemDataComparisonResult = new List<EcuDataComparisonResult>();
        }

        public bool IsEmpty => MasterDataMissingInFirst.Count == 0 && MasterDataMissingInSecond.Count == 0 && MasterDataComparisonResult.Count == 0 &&
            SubsystemDataMissingInFirst?.Count == 0 && SubsystemDataMissingInSecond?.Count == 0 && SubsystemDataComparisonResult?.Count == 0;
    }



    public class EcuDataComparisonResult
    {
        public List<String> FieldsMissingInSecond { get; set; }
        public List<String> FieldsMissingInFirst { get; set; }

        public List<DifferenceMessage> Messages { get; set; }
        public EcuData First { get; set; }
        public EcuData Second { get; set; }


        public EcuDataComparisonResult()
        {
            Messages = new List<DifferenceMessage>();
        }

        public bool IsEmpty => FieldsMissingInFirst.Count == 0 && FieldsMissingInSecond.Count == 0 && Messages.Count == 0;
    }



    public class DifferenceMessage
    {
        public String Path { get; set; }
        public String FieldName { get; set; }
        public String Message { get; set; }
    }
}