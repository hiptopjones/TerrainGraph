using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(TerrainDataWrapper))]
    public class TerrainDataWrapperDrawer : PropertyDrawer
    {
        private List<TerrainData> _terrainDatas = new();
        private List<string> _terrainDataNames = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            UpdateTerrainDataList();

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;

            var popup = new PopupField<string>(_terrainDataNames, 0);
            popup.RegisterValueChangedCallback(e =>
            {
                // No refresh performed here

                int selectedIndex = popup.choices.IndexOf(e.newValue);
                if (selectedIndex >= 0)
                {
                    var selectedTerrainData = _terrainDatas[selectedIndex];
                    SetSelectedTerrain(property, selectedTerrainData);
                }
                else
                {
                    SetSelectedTerrain(property, null);
                }
            });

            SetSelectedTerrain(property, _terrainDatas.FirstOrDefault());

            container.Add(popup);

            var button = new Button();
            button.text = "Refresh";
            button.clicked += () =>
            {
                // Save the user's selection
                var selectedIndex = popup.index;
                string selectedTerrainName = null;
                if (selectedIndex >= 0)
                {
                    selectedTerrainName = popup.choices[selectedIndex];
                }

                UpdateTerrainDataList();

                popup.choices = _terrainDataNames;

                // Repopulate the user's selection if possible
                if (!string.IsNullOrEmpty(selectedTerrainName))
                {
                    selectedIndex = _terrainDataNames.IndexOf(selectedTerrainName);
                    if (selectedIndex >= 0)
                    {
                        SetSelectedTerrain(property, _terrainDatas[selectedIndex]);
                    }
                    else
                    {
                        selectedIndex = -1;
                        SetSelectedTerrain(property, null);
                    }
                }

                popup.index = Mathf.Clamp(selectedIndex, 0, popup.choices.Count);
            };

            container.Add(button);

            return container;
        }

        private void UpdateTerrainDataList()
        {
            _terrainDatas.Clear();

            var guids = AssetDatabase.FindAssets("t:TerrainData");
            foreach (var guid in guids)
            {
                string assetFilePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetFilePath.StartsWith("Assets"))
                {
                    continue;

                }
                TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(assetFilePath);

                _terrainDatas.Add(terrainData);
            }

            _terrainDatas = _terrainDatas.OrderBy(x => x.name).ToList();
            _terrainDataNames = _terrainDatas.Select(x => x.name).ToList();
        }

        private void SetSelectedTerrain(SerializedProperty property, TerrainData selectedTerrainData)
        {
            if (selectedTerrainData != null)
            {
                Selection.activeObject = selectedTerrainData;
            }

            var terrainDataWrapper = new TerrainDataWrapper { TerrainData = selectedTerrainData };
            property.boxedValue = terrainDataWrapper;

            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        }
    }
}
