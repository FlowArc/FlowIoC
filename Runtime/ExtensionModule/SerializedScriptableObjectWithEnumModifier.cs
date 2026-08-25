#if ODIN_INSPECTOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlowIoC.ExtensionModule
{
    public class SerializedScriptableObjectWithEnumModifier : SafeSerializedScriptableObject
    {
        #if UNITY_EDITOR
        private string _contextPath = "Assets/Scripts/Runtime/Contexts";
        [PropertySpace(10)]
        [Title("Enum Modifier",TitleAlignment = TitleAlignments.Centered)]
        [BoxGroup("Enum",showLabel:false)][PropertyOrder(90)]
        [Sirenix.OdinInspector.FilePath(Extensions = "cs",RequireExistingPath = true,ParentFolder = "$_contextPath")]
        #endif
        public string EnumFile;

        #if UNITY_EDITOR
        [BoxGroup("Enum", showLabel: false)]
        [PropertyOrder(90)]
        
        private string _infoBoxMessage = string.Empty;

        private bool _showMessage = false;
         
        [PropertySpace(20)]
        [InfoBox("$_infoBoxMessage",InfoMessageType.Warning,"_showMessage")]
        [PropertyOrder(90)]
        [BoxGroup("Enum")]
        [ShowInInspector][OnValueChanged("EnumValueChanged")]
        private string _value;
        
        [PropertyOrder(90)]
        [BoxGroup("Enum")]
        [Button(ButtonSizes.Gigantic),GUIColor(.5f,1f,.5f)]
        protected void AddValueToEnum()
        {
            if (_value == String.Empty)
            {
                ShowMessage("Enum Value Cannot Be Empty");
                return;
            }

            string path = _contextPath + "/" + EnumFile;
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (obj == null)
            {
                ShowMessage("There is No Enum File");
                return;
            }
            
            string data = LoadFileOnPath(path);
            
            Regex r=new Regex(@"\s+");
            string searcher=r.Replace(data,";");
            var searchKey = ";"+_value+",;";
            
            if(searcher.Contains(searchKey))
            {
                ShowMessage("Enum already exists");
                return;
            }
            
            string addition="";
            addition+="%Name%,";
            addition+="\r\t\t";
            addition+="ADDPOINT";
            data = data.Replace("//*ADDITION*//",addition);
            data = data.Replace("%Name%", _value);
            data = data.Replace("ADDPOINT","//*ADDITION*//");
            SaveFile(data, path);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ShowMessage("Enum is Added");
            EditorGUIUtility.PingObject(obj);
        }
        #region Editor
        [OnInspectorInit]
        private void OnInspectorInit()
        {
            _value = "";
            EnumValueChanged();
        }
        private void ShowMessage(string text)
        {
            _infoBoxMessage = text;//"Prefab || Key Empty";
            _showMessage = _infoBoxMessage != string.Empty;
        }

        private void EnumValueChanged()
        {
            ShowMessage("");
        }
        #endregion
        private void SaveFile(string data, string filename)
        {
            using (StreamWriter outfile = new StreamWriter(
                       new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write),
                       Encoding.Default))
            {
                outfile.Write(data);
            }
        }

        private string LoadFileOnPath(string filePath)
        {
            try
            {
                //Debug.Log("Loading File = " + filePath);
                string data = string.Empty;
                string path = filePath;
                StreamReader theReader = new StreamReader(path, Encoding.Default);
                using (theReader)
                {
                    data = theReader.ReadToEnd();
                    theReader.Close();
                }

                return data;
            }
            catch (Exception e)
            {
                //Debug.LogException(e);
                ShowMessage("LoadFileOnPath Exception: " +e.Message);
                return string.Empty;
            }
        }
#endif
    }
}
#endif
