using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StatelessForMAUI.Attributes;

namespace StatelessForMAUI.Pages
{
    internal static class PageStateNameGenerator
    {
        public static string GetPageStateName<T>() => GetPageStateName(typeof(T));
        public static string GetPageStateName(this Page page) => GetPageStateName(page.GetType());
        public static string GetPageTrigger(Type type)
        {
           return GetPageTrigger(GetPageStateName(type));
        }
        public static string GetPageTrigger(string name)
        {
            return $"GoTo{name}";
        }
        public static string GetPageStateName(Type? type)
        {
            if (type is null)
            {
                return "NONE";
            }
            string name = type.Name;
            return name;
        }
    }
}
