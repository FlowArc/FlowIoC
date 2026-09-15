using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Modules.LocalSaveModule.Services
{
    /// <summary>
    /// One JSON object in memory, written to a file of the module's own. Each persisted asset is
    /// a member of that object under the name it was filed on the adapter, so the file reads as
    /// what it is: the game's saved data, one asset after another.
    ///
    /// Newtonsoft does the serializing, not JsonUtility. JsonUtility could not carry a Dictionary
    /// member at all, which made the module's zero-setup promise a promise about a shape of data
    /// rather than about data - and a game that keeps its progress in a dictionary had to flatten
    /// it into two parallel lists to be saved. What Newtonsoft takes from an object is narrowed
    /// back to what Unity itself would have saved by UnityFieldContractResolver, so the widening is
    /// in what can be carried and not in what is.
    ///
    /// The file is opened once, in BeginSession, and reached again only by Flush: a startup that
    /// restores a dozen assets reads the file once, and nothing reaches disk until it is asked.
    ///
    /// Errors go through Debug.LogError rather than FlowLogger on purpose. FlowLogger compiles out
    /// of a release build, and a save that silently does nothing is the one failure this module
    /// must never hide - it looks exactly like success until the player restarts.
    /// </summary>
    public class LocalSaveService : ILocalSaveService
    {
        /// <summary>
        /// The file every game gets, under the persistent data path. The extension is FlowIoC's
        /// own rather than .json, so the file does not invite the player to open it in the editor
        /// their system pairs with that name.
        /// </summary>
        internal const string FILE_NAME = "SaveFile.flowsave";

        private const string LOG_PREFIX = "[LocalSave] ";

        private readonly LocalSaveFile _file;

        private readonly JsonSerializer _serializer;

        private JObject _entries = new JObject();

        private string _password;

        public LocalSaveService() : this(FILE_NAME)
        {
        }

        /// <summary>Lets a test point at a file of its own instead of the player's save.</summary>
        public LocalSaveService(string fileName)
        {
            _file = new LocalSaveFile(fileName);

            _serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                ContractResolver = new UnityFieldContractResolver(),

                // A field declared as an interface or a base class writes what it actually holds,
                // which is what makes a [SerializeReference] field survive the round trip. The
                // binder decides what such a name is allowed to build.
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new LocalSaveTypeBinder(),

                // Loading overwrites, so a collection is replaced rather than added to. Without
                // this an entry the player deleted would come back on every load, for ever.
                ObjectCreationHandling = ObjectCreationHandling.Replace,

                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        public void BeginSession(string password)
        {
            _password = password;
            _entries = new JObject();

            try
            {
                // A first run has no file yet. That is not a failure: it is what "nothing saved
                // so far" looks like, and ReadText answers it with null.
                string text = _file.ReadText(password);

                if (!string.IsNullOrEmpty(text))
                    _entries = JObject.Parse(text);
            }
            catch (Exception exception)
            {
                _entries = new JObject();

                Debug.LogError(LOG_PREFIX + "BeginSession"
                                          + " failed, starting from an empty save. " + exception);
            }
        }

        public bool Has(string key) => _entries.ContainsKey(key);

        public void LoadInto(string key, object target)
        {
            try
            {
                if (!_entries.TryGetValue(key, out JToken token) || token.Type == JTokenType.Null)
                    return;

                using (JsonReader reader = token.CreateReader())
                    _serializer.Populate(reader, target);
            }
            catch (Exception exception)
            {
                Debug.LogError(LOG_PREFIX + "LoadInto" + " failed for '" + key + "'. " + exception);
            }
        }

        public void Save(string key, object value)
        {
            try
            {
                _entries[key] = JToken.FromObject(value, _serializer);
            }
            catch (Exception exception)
            {
                Debug.LogError(LOG_PREFIX + "Save" + " failed for '" + key + "'. " + exception);
            }
        }

        public void Flush()
        {
            try
            {
                _file.WriteText(_entries.ToString(Formatting.Indented), _password);
            }
            catch (Exception exception)
            {
                Debug.LogError(LOG_PREFIX + "Flush"
                                          + " failed, this session was not written. " + exception);
            }
        }
    }
}