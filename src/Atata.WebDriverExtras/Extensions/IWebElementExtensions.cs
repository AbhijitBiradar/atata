﻿using OpenQA.Selenium;
using System;

namespace Atata
{
    // TODO: Review IWebElementExtensions class. Remove unused methods.
    public static class IWebElementExtensions
    {
        public static WebElementExtendedSearchContext Try(this IWebElement element)
        {
            return new WebElementExtendedSearchContext(element);
        }

        public static WebElementExtendedSearchContext Try(this IWebElement element, TimeSpan timeout)
        {
            return new WebElementExtendedSearchContext(element, timeout);
        }

        public static WebElementExtendedSearchContext Try(this IWebElement element, TimeSpan timeout, TimeSpan retryInterval)
        {
            return new WebElementExtendedSearchContext(element, timeout, retryInterval);
        }

        public static bool HasContent(this IWebElement element, string content)
        {
            return element.Text.Contains(content);
        }

        public static string GetValue(this IWebElement element)
        {
            return element.GetAttribute("value");
        }

        public static IWebElement FillInWith(this IWebElement element, string text)
        {
            element.Clear();
            if (!string.IsNullOrEmpty(text))
                element.SendKeys(text);
            return element;
        }
    }
}