using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDB.Models
{
    public class SelectListUtilities<T>
    {
        public static SelectList Convert(IEnumerable<T> collection, string targetField = "Name", string defaultText = "")
        {
            List<SelectListItem> items = new List<SelectListItem>();
            if (typeof(T).Name == "String")
            {
                int index = 0;
                foreach (T item in collection)
                {
                    items.Add(new SelectListItem() { Value = index.ToString(), Text = item.ToString() });
                    index++;
                }
            }
            else
            {
                foreach (T item in collection)
                {
                    items.Add(
                        new SelectListItem()
                        {
                            Value = typeof(T).GetProperty("Id").GetValue(item, null).ToString(),
                            Text = typeof(T).GetProperty(targetField).GetValue(item, null).ToString()
                        });
                }
            }
            if (defaultText != "")
                items.Insert(0, new SelectListItem { Value = "0", Text = defaultText });
            return new SelectList(items, "Value", "Text", 0, new[] { 0 });
        }
    }
}