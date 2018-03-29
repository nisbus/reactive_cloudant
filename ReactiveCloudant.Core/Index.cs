using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ReactiveCloudant
{
    /// <summary>
    /// A list of indexes
    /// </summary>
    public class IndexList
    {
        /// <summary>
        /// A collection of <see cref="Index"/>
        /// </summary>
        [JsonProperty(PropertyName="indexes")]
        public List<Index> Indexes { get; set; }
    }

    /// <summary>
    /// An index definition
    /// </summary>
    public class IndexDefinition
    {
        [JsonExtensionData]
        [JsonProperty(PropertyName = "fields")]
        internal IDictionary<string, JToken> fields { get; set; }

        /// <summary>
        /// Creates a new IndexDefinition
        /// </summary>
        public IndexDefinition()
        {
            fields = new Dictionary<string, JToken>();
        }

        /// <summary>
        /// The fields to index
        /// </summary>
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

    /// <summary>
    /// A class to hold index definitions
    /// </summary>
    public class Index
    {
        /// <summary>
        /// The design document that holds the index
        /// </summary>
        [JsonProperty(PropertyName="ddoc")]
        public string DesignDoc { get; set; }

        /// <summary>
        /// The name of the index
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The type of the index
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        /// <summary>
        /// The definition of the index <see cref="IndexDefinition"/>
        /// </summary>
        [JsonProperty(PropertyName = "def")]
        private IndexDefinition Definition { get; set; }

        /// <summary>
        /// The fields indexed in the Definition
        /// </summary>
        public List<IndexField> Fields { get { return Definition.Fields; } }

        /// <summary>
        /// Creates a new Index
        /// </summary>
        public Index()
        {
            Definition = new IndexDefinition();
        }

        /// <summary>
        /// Adds a field to the index
        /// </summary>
        /// <param name="field">The field to index</param>
        public void AddField(IndexField field)
        {
            if (!string.IsNullOrWhiteSpace(field.FieldName) && !Definition.fields.ContainsKey(field.FieldName))
            {
                var a = new JArray
                {
                    JObject.Parse("{\"" + field.FieldName + "\":\"" + field.SortOrder + "\"}")
                };
                Definition.fields.Add(field.FieldName, a);
            }
        }
    }


    /// <summary>
    /// Holder of Index fields
    /// </summary>
    public class IndexField
    {
        /// <summary>
        /// The name of the field to index
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// The sort order (asc/desc)
        /// </summary>
        public string SortOrder { get; set; }

        /// <summary>
        /// Creates a new IndexField
        /// </summary>
        public IndexField()
        {
            SortOrder = "asc";
        }
    }
}
