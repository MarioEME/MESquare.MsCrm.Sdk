using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Metadata;

namespace MESquare.MsCrm.Sdk.Extentions
{
    public static class AttributeMetadataExt
    {
        public static String DisplayLabel(this AttributeMetadata attribute)
        {
            return attribute.DisplayName?.UserLocalizedLabel?.Label ?? $"[{attribute.LogicalName}]";
        }
    }
}
