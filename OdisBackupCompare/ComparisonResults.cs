using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OdisBackupCompare
{
    public class ComparisonResults
    {
        public Dictionary<String, Ecu> EcusMissingInFirst { get; set; }
        public Dictionary<String, Ecu> EcusMissingInSecond { get; set; }

        public DictionaryList<EcuComparisonResult, String> EcusComparisonResult { get; set; }


        public ComparisonResults()
        {
            EcusComparisonResult = new DictionaryList<EcuComparisonResult, String>((ecu, uniqueIndex) => ecu.EcuId);
            EcusMissingInFirst = new Dictionary<String, Ecu>();
            EcusMissingInSecond = new Dictionary<String, Ecu>();
        }
    }



    public class EcuComparisonResult
    {
        public String EcuId { get; set; }
        public String[] EcuNames => new String[] { First.ToString(), Second.ToString() }.Distinct().ToArray();

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
        public List<String> Path { get; set; }

        public List<String> Descriptions { get; set; }

        public Dictionary<String, ValueItem> FieldsMissingInFirst { get; set; }
        public Dictionary<String, ValueItem> FieldsMissingInSecond { get; set; }

        public List<DifferenceMessage> Differences { get; set; }

        [JsonIgnore]
        public EcuData First { get; set; }
        [JsonIgnore]
        public EcuData Second { get; set; }


        public EcuDataComparisonResult()
        {
            Differences = new List<DifferenceMessage>();

            Path = new List<String>();
            FieldsMissingInFirst = new Dictionary<String, ValueItem>();
            FieldsMissingInSecond = new Dictionary<String, ValueItem>();
        }

        [JsonIgnore]
        public bool IsEmpty => FieldsMissingInFirst.Count == 0 && FieldsMissingInSecond.Count == 0 && Differences.Count == 0;
    }



    public class DifferenceMessage
    {
        public List<String> Path { get; set; }
        public List<String> FieldDescriptions { get; set; }
        public FieldPropertyEnum FieldProperty { get; set; }
        public String Message { get; set; }
        public String FirstValue { get; set; }
        public String SecondValue { get; set; }

        public DifferenceMessage()
        {
            Path = new List<String>();
        }
    }
}
