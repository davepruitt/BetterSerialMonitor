using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterSerialMonitor.Utilities
{
    /// <summary>
    /// This class defines an attribute that view-model properties can use to react to changes
    /// on properties in the model
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ReactToModelPropertyChanged : System.Attribute
    {
        public List<string> ModelPropertyNames = new List<string>();

        public ReactToModelPropertyChanged(string[] propertyNames)
        {
            ModelPropertyNames.AddRange(propertyNames);
        }
    }
}
