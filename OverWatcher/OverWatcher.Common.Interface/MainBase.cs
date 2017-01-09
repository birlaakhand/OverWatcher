using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.Linq;

namespace OverWatcher.Common.Interface
{
    public class MainBase
    {
        public static string ProbingPath { get; private set; }
        static MainBase()
        {
            ProbingPath = GetProbingPath();
        }
        private static string GetProbingPath()
        {
            var configFile = XElement.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            var probingElement = (
                from runtime
                    in configFile.Descendants("runtime")
                from assemblyBinding
                    in runtime.Elements(XName.Get("assemblyBinding", "urn:schemas-microsoft-com:asm.v1"))
                from probing
                    in assemblyBinding.Elements(XName.Get("probing", "urn:schemas-microsoft-com:asm.v1"))
                select probing)
                .FirstOrDefault();

            return probingElement?.Attribute("privatePath").Value;
        }
    }
}
