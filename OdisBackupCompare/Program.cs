using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OdisBackupCompare
{
    public class Program
    {
        public static int Main(string[] args)
        {

            var file1 = "SampleXML\\FullBackup_PXC_WVWZZZAUZEW070736_20250315T173802.xml";
            var file2 = "SampleXML\\FullBackup_PXC_WVWZZZAUZHP345715_20250912T154832.xml";

            var odisDataOne = OdisData.ParseFromFile(file1);
            var odisDataTwo = OdisData.ParseFromFile(file2);

            var ecusOne = odisDataOne.GetEcus();
            var ecusTwo = odisDataTwo.GetEcus();

            var odisDataComparer = new OdisDataComparer(new OdisDataComparerSettings { FieldToBypassOnComparison = new List<FieldType> { FieldType.DisplayName } });

            var compareResult = odisDataComparer.CompareEcus(ecusOne, ecusTwo);

            return 0;
        }


    }
}