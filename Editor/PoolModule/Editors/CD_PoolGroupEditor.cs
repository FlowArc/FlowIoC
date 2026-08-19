#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FlowIoC.PoolModule.Data.UnityObjects;
using UnityEditorInternal;

namespace FlowIoC.Editor.PoolModule.Editors
{
    [CustomEditor(typeof(CD_PoolGroup))]
    public class CD_PoolGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty _itemListProp;

        private GUIStyle _headerStyle;
        private GUIStyle _boldLabelStyle;

        private bool _stylesInitialized;

        private ReorderableList _reorderableList;

        private readonly GUIContent _poolKeyContent = new("Pool Key", "Unique identifier for this pool item");
        private readonly GUIContent _prefabContent = new("Prefab", "GameObject prefab to pool");
        private readonly GUIContent _addressablePrefabContent = new("Addressable Prefab", "Addressable asset reference to pool");
        private readonly GUIContent _isAddressableContent = new("Is Addressable", "Whether the asset is Addressable");
        private readonly GUIContent _initialCountContent = new("Initial Count", "Number of instances to create at initialization");
        private readonly GUIContent _isExtendableContent = new("Is Extendable", "Whether the pool can grow beyond initial size");
        private readonly GUIContent _lazyLoadContent = new("Lazy Load", "Create instances only when needed");

        private void OnEnable()
        {
            _itemListProp = serializedObject.FindProperty("_items");

            if (_itemListProp == null)
            {
                Debug.LogError("Failed to find _itemList property in CD_PoolGroup");
                return;
            }

            _reorderableList = new ReorderableList(serializedObject, _itemListProp, true, true, true, true);

            _reorderableList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Pool Items"); };

            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                DrawPoolItemElement(rect, element, index, isActive, isFocused);
            };

            _reorderableList.elementHeightCallback = index =>
            {
                SerializedProperty element = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                return GetPoolItemElementHeight(element);
            };

            _reorderableList.onAddCallback = list =>
            {
                int newIndex = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = newIndex;

                SerializedProperty newItem = list.serializedProperty.GetArrayElementAtIndex(newIndex);
                InitializeNewPoolItem(newItem, newIndex);
                newItem.isExpanded = true;
            };

            _reorderableList.onRemoveCallback = list =>
            {
                if (EditorUtility.DisplayDialog("Remove Item",
                        "Are you sure you want to remove this pool item?",
                        "Remove", "Cancel"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
        }

        private void InitStyles()
        {
            if (_stylesInitialized)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _boldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 5, 3)
            };

            _stylesInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            InitStyles();

            serializedObject.Update();

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Pool Group Configuration", _headerStyle);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (_reorderableList != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUI.BeginDisabledGroup(_reorderableList.index < 0 || _reorderableList.index >= _reorderableList.count);
                if (GUILayout.Button(new GUIContent("Duplicate Selected", "Duplicate selected item"), GUILayout.ExpandWidth(false)))
                {
                    DuplicateSelectedItemInList();
                }

                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);

                _reorderableList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPoolItemElement(Rect rect, SerializedProperty element, int index, bool isActive, bool isFocused)
        {
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float propertySpacing = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty keyProp = element.FindPropertyRelative("PoolKey");
            SerializedProperty prefabProp = element.FindPropertyRelative("Prefab");
            SerializedProperty addressablePrefabProp = element.FindPropertyRelative("AddressablePrefab");
            SerializedProperty isAddressableProp = element.FindPropertyRelative("IsAddressable");
            SerializedProperty initialCountProp = element.FindPropertyRelative("InitialCreateCount");
            SerializedProperty isExtendableProp = element.FindPropertyRelative("IsExtendable");
            SerializedProperty lazyLoadProp = element.FindPropertyRelative("LazyLoad");

            string itemName = keyProp != null && !string.IsNullOrEmpty(keyProp.stringValue)
                ? keyProp.stringValue
                : $"Item {index}";
            string prefabName = "";
            if (prefabProp != null && prefabProp.objectReferenceValue != null)
            {
                GameObject prefabObj = prefabProp.objectReferenceValue as GameObject;
                prefabName = prefabObj != null ? $" [{prefabObj.name}]" : "";
            }

            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, singleLineHeight);
            element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, itemName + prefabName, true);

            float currentY = rect.y + singleLineHeight + propertySpacing;

            if (element.isExpanded)
            {
                float originalLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = rect.width * 0.4f;

                EditorGUI.indentLevel++;

                Rect idLabelRect = new Rect(rect.x, currentY, rect.width, singleLineHeight);
                EditorGUI.LabelField(idLabelRect, "Identification", _boldLabelStyle);
                currentY += singleLineHeight + propertySpacing;

                Rect keyRect = new Rect(rect.x, currentY, rect.width, EditorGUI.GetPropertyHeight(keyProp, _poolKeyContent, true));
                EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none, true);
                currentY += EditorGUI.GetPropertyHeight(keyProp, _poolKeyContent, true) + propertySpacing;

                Rect assetLabelRect = new Rect(rect.x, currentY, rect.width, singleLineHeight);
                EditorGUI.LabelField(assetLabelRect, "Asset Config", _boldLabelStyle);
                currentY += singleLineHeight + propertySpacing;

                Rect toggleRect = new Rect(rect.x, currentY, rect.width, singleLineHeight);
                EditorGUI.PropertyField(toggleRect, isAddressableProp, _isAddressableContent);
                currentY += singleLineHeight + propertySpacing;

                if (isAddressableProp.boolValue)
                {
                    Rect addrRect = new Rect(rect.x, currentY, rect.width, EditorGUI.GetPropertyHeight(addressablePrefabProp, _addressablePrefabContent, true));
                    EditorGUI.PropertyField(addrRect, addressablePrefabProp, _addressablePrefabContent, true);
                    currentY += EditorGUI.GetPropertyHeight(addressablePrefabProp, _addressablePrefabContent, true) + propertySpacing;
                }
                else
                {
                    Rect prefabRect = new Rect(rect.x, currentY, rect.width, EditorGUI.GetPropertyHeight(prefabProp, _prefabContent, true));
                    EditorGUI.PropertyField(prefabRect, prefabProp, _prefabContent, true);
                    currentY += EditorGUI.GetPropertyHeight(prefabProp, _prefabContent, true) + propertySpacing;
                }

                Rect poolCfgLabelRect = new Rect(rect.x, currentY, rect.width, singleLineHeight);
                EditorGUI.LabelField(poolCfgLabelRect, "Pool Config", _boldLabelStyle);
                currentY += singleLineHeight + propertySpacing;

                if (initialCountProp != null)
                {
                    Rect countRect = new Rect(rect.x, currentY, rect.width, singleLineHeight);
                    int newCount = EditorGUI.IntField(countRect, _initialCountContent, initialCountProp.intValue);
                    initialCountProp.intValue = Mathf.Max(0, newCount);
                    currentY += singleLineHeight + propertySpacing;
                }

                if (isExtendableProp != null)
                {
                    Rect extendableRect = new Rect(rect.x, currentY, rect.width, EditorGUI.GetPropertyHeight(isExtendableProp, _isExtendableContent, true));
                    EditorGUI.PropertyField(extendableRect, isExtendableProp, _isExtendableContent, true);
                    currentY += EditorGUI.GetPropertyHeight(isExtendableProp, _isExtendableContent, true) + propertySpacing;
                }

                if (lazyLoadProp != null)
                {
                    Rect lazyLoadRect = new Rect(rect.x, currentY, rect.width, EditorGUI.GetPropertyHeight(lazyLoadProp, _lazyLoadContent, true));
                    EditorGUI.PropertyField(lazyLoadRect, lazyLoadProp, _lazyLoadContent, true);
                    currentY += EditorGUI.GetPropertyHeight(lazyLoadProp, _lazyLoadContent, true) + propertySpacing;
                }

                if (keyProp != null && string.IsNullOrEmpty(keyProp.stringValue))
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, singleLineHeight * 1.8f);
                    EditorGUI.HelpBox(warningRect, "Pool Key is required.", MessageType.Warning);
                    currentY += singleLineHeight * 1.8f + propertySpacing;
                }

                if (isAddressableProp.boolValue)
                {
                    SerializedProperty guidProp = addressablePrefabProp?.FindPropertyRelative("m_AssetGUID");
                    if (guidProp == null || string.IsNullOrEmpty(guidProp.stringValue))
                    {
                        Rect warningRect = new Rect(rect.x, currentY, rect.width, singleLineHeight * 1.8f);
                        EditorGUI.HelpBox(warningRect, "Addressable prefab reference is required.", MessageType.Warning);
                    }
                }
                else if (prefabProp != null && prefabProp.objectReferenceValue == null)
                {
                    Rect warningRect = new Rect(rect.x, currentY, rect.width, singleLineHeight * 1.8f);
                    EditorGUI.HelpBox(warningRect, "Prefab reference is required.", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
        }

        private float GetPoolItemElementHeight(SerializedProperty element)
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (element.isExpanded)
            {
                SerializedProperty keyProp = element.FindPropertyRelative("PoolKey");
                SerializedProperty prefabProp = element.FindPropertyRelative("Prefab");
                SerializedProperty addressablePrefabProp = element.FindPropertyRelative("AddressablePrefab");
                SerializedProperty isAddressableProp = element.FindPropertyRelative("IsAddressable");
                SerializedProperty initialCountProp = element.FindPropertyRelative("InitialCreateCount");
                SerializedProperty isExtendableProp = element.FindPropertyRelative("IsExtendable");
                SerializedProperty lazyLoadProp = element.FindPropertyRelative("LazyLoad");

                height += 3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);


                height += EditorGUI.GetPropertyHeight(keyProp, _poolKeyContent, true) + EditorGUIUtility.standardVerticalSpacing;

                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // toggle

                if (isAddressableProp.boolValue)
                {
                    height += EditorGUI.GetPropertyHeight(addressablePrefabProp, _addressablePrefabContent, true) + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    height += EditorGUI.GetPropertyHeight(prefabProp, _prefabContent, true) + EditorGUIUtility.standardVerticalSpacing;
                }

                if (initialCountProp != null) height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (isExtendableProp != null) height += EditorGUI.GetPropertyHeight(isExtendableProp, _isExtendableContent, true) + EditorGUIUtility.standardVerticalSpacing;

                if (lazyLoadProp != null) height += EditorGUI.GetPropertyHeight(lazyLoadProp, _lazyLoadContent, true) + EditorGUIUtility.standardVerticalSpacing;

                bool keyWarning = keyProp != null && string.IsNullOrEmpty(keyProp.stringValue);
                bool prefabWarning = false;
                if (isAddressableProp.boolValue)
                {
                    var guidProp = addressablePrefabProp.FindPropertyRelative("m_AssetGUID");
                    prefabWarning = guidProp == null || string.IsNullOrEmpty(guidProp.stringValue);
                }
                else
                {
                    prefabWarning = prefabProp != null && prefabProp.objectReferenceValue == null;
                }

                if (keyWarning) height += EditorGUIUtility.singleLineHeight * 1.8f + EditorGUIUtility.standardVerticalSpacing;
                if (prefabWarning) height += EditorGUIUtility.singleLineHeight * 1.8f + EditorGUIUtility.standardVerticalSpacing;

                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        private void InitializeNewPoolItem(SerializedProperty newItem, int index)
        {
            if (newItem != null)
            {
                SerializedProperty keyProp = newItem.FindPropertyRelative("PoolKey");
                SerializedProperty initialCountProp = newItem.FindPropertyRelative("InitialCreateCount");
                SerializedProperty isExtendableProp = newItem.FindPropertyRelative("IsExtendable");
                SerializedProperty lazyLoadProp = newItem.FindPropertyRelative("LazyLoad");
                SerializedProperty prefabProp = newItem.FindPropertyRelative("Prefab");
                SerializedProperty addressablePrefabProp = newItem.FindPropertyRelative("AddressablePrefab");
                SerializedProperty isAddressableProp = newItem.FindPropertyRelative("IsAddressable");

                if (keyProp != null)
                    keyProp.stringValue = $"Item{index}";

                if (initialCountProp != null)
                    initialCountProp.intValue = 10;

                if (isExtendableProp != null)
                    isExtendableProp.boolValue = true;

                if (lazyLoadProp != null)
                    lazyLoadProp.boolValue = false;

                if (isAddressableProp != null)
                    isAddressableProp.boolValue = false;

                if (addressablePrefabProp != null)
                {
                    var guidProp = addressablePrefabProp.FindPropertyRelative("m_AssetGUID");
                    if (guidProp != null) guidProp.stringValue = string.Empty;
                }

                if (prefabProp != null)
                    prefabProp.objectReferenceValue = null;
            }
        }

        private void DuplicateSelectedItemInList()
        {
            int selectedIndex = _reorderableList.index;
            if (selectedIndex < 0 || selectedIndex >= _reorderableList.count)
                return;

            _itemListProp.InsertArrayElementAtIndex(selectedIndex + 1);
            _reorderableList.index = selectedIndex + 1;

            SerializedProperty srcItem = _itemListProp.GetArrayElementAtIndex(selectedIndex);
            SerializedProperty newItem = _itemListProp.GetArrayElementAtIndex(selectedIndex + 1);

            if (srcItem != null && newItem != null)
            {
                CopyItemProperties(srcItem, newItem);

                SerializedProperty newKeyProp = newItem.FindPropertyRelative("PoolKey");
                if (newKeyProp != null)
                {
                    newKeyProp.stringValue += "_Copy";
                }

                newItem.isExpanded = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CopyItemProperties(SerializedProperty source, SerializedProperty dest)
        {
            CopyProperty(source, dest, "PoolKey", true);
            CopyProperty(source, dest, "Prefab", false);
            CopyProperty(source, dest, "AddressablePrefab", false);
            CopyProperty(source, dest, "IsAddressable", false);
            CopyProperty(source, dest, "InitialCreateCount", false);
            CopyProperty(source, dest, "IsExtendable", false);
            CopyProperty(source, dest, "LazyLoad", false);
        }

        private void CopyProperty(SerializedProperty sourceParent, SerializedProperty destParent, string propertyName, bool isString)
        {
            SerializedProperty srcProp = sourceParent.FindPropertyRelative(propertyName);
            SerializedProperty destProp = destParent.FindPropertyRelative(propertyName);

            if (srcProp != null && destProp != null)
            {
                if (isString)
                    destProp.stringValue = srcProp.stringValue;
                else if (srcProp.propertyType == SerializedPropertyType.ObjectReference)
                    destProp.objectReferenceValue = srcProp.objectReferenceValue;
                else if (srcProp.propertyType == SerializedPropertyType.Integer)
                    destProp.intValue = srcProp.intValue;
                else if (srcProp.propertyType == SerializedPropertyType.Boolean)
                    destProp.boolValue = srcProp.boolValue;
            }
        }
    }
}
#endif