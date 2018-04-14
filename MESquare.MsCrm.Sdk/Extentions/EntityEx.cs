using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace MESquare.MsCrm.Sdk.Extentions
{
    public static class EntityEx
    {
        public static String GetAttributeText(this Entity entity, String attributeName)
        {
            var valueAsString = String.Empty;

            if (entity.Contains(attributeName))
            {
                var value = entity.Attributes[attributeName];

                if (value is OptionSetValue)
                {
                    valueAsString = entity.FormattedValues[attributeName];
                }
                else if (value is EntityReference)
                {
                    valueAsString = (value as EntityReference).Name?.ToString();
                }
                else if (value is Money)
                {
                    valueAsString = (value as Money).Value.ToString();
                }
                else
                {
                    valueAsString = value.ToString();
                }
            }

            return valueAsString;
        }

        public static String GetAttributeValueAsString(this Entity entity, String attributeName)
        {
            var valueAsString = String.Empty;

            if (entity.Contains(attributeName))
            {
                var value = entity.Attributes[attributeName];

                if (value is OptionSetValue)
                {
                    valueAsString = (value as OptionSetValue).Value.ToString();
                }
                else if (value is Money)
                {
                    valueAsString = (value as Money).Value.ToString();
                }
            }

            return valueAsString;
        }
    }
}
