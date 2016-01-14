﻿using Humanizer;
using System.Linq;
using System.Text;

namespace Atata
{
    public class FindByIdStrategy : SimpleElementFindStrategy
    {
        public FindByIdStrategy()
            : base(applyIndex: false)
        {
        }

        protected override void BuildXPath(StringBuilder builder, ElementFindOptions options)
        {
            string idCondition = string.IsNullOrWhiteSpace(options.IdFinderFormat)
                ? options.GetQualifiersXPathCondition("@id")
                : string.Join(" or ", options.Qualifiers.Select(x => options.IdFinderFormat.FormatWith(x)));

            builder.Insert(0, "*[{0}]{1}/descendant-or-self::".FormatWith(idCondition, options.GetPositionWrappedXPathCondition()));
        }
    }
}