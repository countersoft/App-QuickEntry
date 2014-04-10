using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace QuickEntry
{
    public class QuickEntryModel
    {
        public IEnumerable<SelectListItem> ProjectList { get; set; }
    }

    public class Item
    {
        public string text { get; set; }
        public List<Item> children { get; set; }
    }
}
