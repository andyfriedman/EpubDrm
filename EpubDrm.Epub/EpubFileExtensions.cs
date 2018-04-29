using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EpubDrm.Epub
{
    public static class EpubFileExtensions
    {
        public static IEnumerable<XElement> ByAttributeValue(this IEnumerable<XElement> xElements, string attributeName, string attributeValue)
        {
            return (from xElement in xElements
                    where xElement.HasAttributes
                    let attribute = xElement.Attributes().SingleOrDefault(x => x.Name == attributeName)
                    where attribute != null && attribute.Value == attributeValue
                    select xElement);
        }
    }
}
