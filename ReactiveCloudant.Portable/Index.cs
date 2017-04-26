using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveCloudant
{
    public class IndexList
    {
        [JsonProperty(PropertyName="indexes")]
        public List<Index> Indexes { get; set; }
    }

    public class IndexDefinition
    {
        [JsonExtensionData]
        [JsonProperty(PropertyName = "fields")]
        internal IDictionary<string, JToken> fields { get; set; }
        public IndexDefinition()
        {
            fields = new Dictionary<string, JToken>();
        }

        public List<IndexField> Fields
        {
            get
            {
                List<IndexField> fs = new List<IndexField>();
                if (fields != null)
                {
                    foreach (var f in fields)
                    {
                        var o = f.Value as JArray;
                        foreach (JObject a in o)
                        {
                            foreach(var p in a.Properties())
                                fs.Add(new IndexField { FieldName = p.Name, SortOrder = p.Value.ToString() });
                        }                        
                    }
                }
                return fs;
            }
        }
    }

    public class Index
    {
        [JsonProperty(PropertyName="ddoc")]
        public string DesignDoc { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "def")]
        private IndexDefinition Definition { get; set; }
        public List<IndexField> Fields { get { return Definition.Fields; } }

        public Index()
        {
            Definition = new IndexDefinition();
        }

        public void AddField(IndexField field)
        {
            if (!string.IsNullOrWhiteSpace(field.FieldName) && !Definition.fields.ContainsKey(field.FieldName))
            {
                var a = new JArray();
                a.Add(JObject.Parse("{\"" + field.FieldName + "\":\"" + field.SortOrder + "\"}"));
                Definition.fields.Add(field.FieldName, a);
            }
        }
    }

    public class IndexField
    {
        public string FieldName { get; set; }
        public string SortOrder { get; set; }
        public IndexField()
        {
            SortOrder = "asc";
        }
    }
}
