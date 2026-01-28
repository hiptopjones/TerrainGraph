using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(BehaviorInjector))]
    public class BehaviorInjectorDrawer : PropertyDrawer
    {
        private List<VisualElement> _injectedElements = new();

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            // If the target is null, don't bother registering attach/detach
            // The target can be null for the following reasons:
            //  - Node list previews
            //  - Graphs serialized prior to the injector's addition
            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as BehaviorInjector;
            if (target != null)
            {
                root.RegisterCallback<AttachToPanelEvent>(_ => AddInjectedBehavior(property, root));
                root.RegisterCallback<DetachFromPanelEvent>(_ => RemoveInjectedBehavior(root));
            }

            return root;
        }

        private void AddInjectedBehavior(SerializedProperty property, VisualElement root)
        {
            var inputsTypeName = property.FindPropertyRelative("InputsTypeName")?.stringValue;
            var optionsTypeName = property.FindPropertyRelative("OptionsTypeName")?.stringValue;

            if (!string.IsNullOrEmpty(inputsTypeName))
            {
                var optionsModel = ClassModelCache.GetClassModel(optionsTypeName);
                var inputsModel = ClassModelCache.GetClassModel(inputsTypeName);

                UpdateFields(root, inputsModel, optionsModel);
            }
            else
            {
                // Try again shortly, when hopefully the node has been able to set the type name
                EditorApplication.delayCall += () => AddInjectedBehavior(property, root);
            }
        }

        private void RemoveInjectedBehavior(VisualElement root)
        {
            foreach (var element in _injectedElements)
            {
                element.parent.Remove(element);
            }

            _injectedElements.Clear();
        }

        private void UpdateFields(VisualElement root, ClassModel inputsModel, ClassModel optionsModel)
        {
            VisualElement optionsRoot = null;
            VisualElement inputsRoot = null;

            if (TryFindAncestorByName(root, "node-options", out optionsRoot))
            {
                var optionsParent = optionsRoot.parent;
                if (optionsParent != null)
                {
                    var portsRoot = optionsParent.Q("port-container");
                    if (portsRoot != null)
                    {
                        inputsRoot = portsRoot.Q("inputs");
                    }
                }
            }

            // Sometimes we get called for attach, but some things are not yet present
            if (optionsRoot != null && inputsRoot != null)
            {
                ProcessOptions(optionsRoot, optionsModel);
                ProcessInputs(inputsRoot, inputsModel);
            }
            else
            {
                // Try again shortly, when hopefully everything is in place
                EditorApplication.delayCall += () => UpdateFields(root, inputsModel, optionsModel);
            }
        }

        private void ProcessOptions(VisualElement optionsRoot, ClassModel optionsModel)
        {
            var labels = optionsRoot.Query<Label>().Build();
            var displayNames = labels.Select(x => x.text).ToList();
            var editors = labels.Select(x => x.parent).ToList();

            for (int i = 0; i < displayNames.Count(); i++)
            {
                var displayName = displayNames[i];
                var editorElement = editors[i];

                var toggleField = editorElement as Toggle;
                if (toggleField != null)
                {
                    var fieldModel = optionsModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                    if (fieldModel.Name == "IsNodeDisabled")
                    {
                        AddDisabledBanner(toggleField, optionsRoot, optionsModel);
                    }
                }
            }
        }

        private void ProcessInputs(VisualElement inputsRoot, ClassModel inputsModel)
        {
            var connectors = inputsRoot.Query("connector-container").Build().ToList();
            var editors = inputsRoot.Query("constant-editor").Build().ToList();

            if (editors.Count == connectors.Count)
            {
                var displayNames = connectors.Select(x => x.Q<Label>().text).ToList();

                for (int i = 0; i < displayNames.Count; i++)
                {
                    var displayName = displayNames[i];
                    var editorElement = editors[i];

                    var floatField = editorElement.Q<FloatField>();
                    if (floatField != null)
                    {
                        var fieldModel = inputsModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                        UpdateFloatField(floatField, fieldModel);

                        continue;
                    }

                    var integerField = editorElement.Q<IntegerField>();
                    if (integerField != null)
                    {
                        var fieldModel = inputsModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                        UpdateIntegerField(integerField, fieldModel);

                        continue;
                    }

                    var textField = editorElement.Q<TextField>();
                    if (textField != null)
                    {
                        var fieldModel = inputsModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                        UpdateTextField(textField, fieldModel);

                        continue;
                    }
                }
            }
        }

        private void AddDisabledBanner(Toggle toggle, VisualElement optionsRoot, ClassModel inputsModel)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    marginLeft = 10,
                    marginTop = 10,
                    marginRight = 10,
                    marginBottom = 10,
                    backgroundColor = Color.yellow,
                }
            };

            var label = new Label("DISABLED")
            {
                style =
                {
                    paddingLeft = 5,
                    paddingTop = 5,
                    paddingRight = 5,
                    paddingBottom = 5,
                    fontSize = 50,
                    color = Color.black,
                    alignSelf = Align.Center
                }
            };
            container.Add(label);

            container.style.display = toggle.value ? DisplayStyle.Flex : DisplayStyle.None;

            toggle.RegisterValueChangedCallback(e =>
            {
                container.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            optionsRoot.Add(container);
            _injectedElements.Add(container);
        }

        private bool TryFindAncestorByName(VisualElement node, string name, out VisualElement ancestor)
        {
            while (true)
            {
                var parent = node.parent;
                if (parent == null)
                {
                    ancestor = null;
                    return false;
                }

                if (parent.name == name)
                {
                    ancestor = parent;
                    return true;
                }

                node = parent;
            }
        }

        private void UpdateTextField(TextField textField, FieldModel fieldModel)
        {
            textField.style.width = 250;
        }

        private void UpdateFloatField(FloatField floatField, FieldModel fieldModel)
        {
            if (!fieldModel.UseSlider)
            {
                return;
            }

            floatField.style.width = 50;
            floatField.parent.style.flexDirection = FlexDirection.RowReverse;

            var container = floatField.parent;

            var rangeMin = (float)fieldModel.Min;
            var rangeMax = (float)fieldModel.Max;

            var slider = new Slider()
            {
                lowValue = rangeMin,
                highValue = rangeMax,
            };
            slider.style.flexGrow = 1;

            Undo.undoRedoPerformed += () =>
            {
                // Schedule update because value may not have actually changed yet
                EditorApplication.delayCall += () => slider.value = floatField.value;
            };

            floatField.RegisterValueChangedCallback(e =>
            {
                var value = Mathf.Clamp(e.newValue, rangeMin, rangeMax);
                floatField.value = value;
                slider.value = value;
            });

            var currentUndoGroup = 0;
            slider.RegisterCallback<MouseCaptureEvent>(e =>
            {
                Undo.SetCurrentGroupName("Update Values");
                currentUndoGroup = Undo.GetCurrentGroup();
            });

            slider.RegisterCallback<MouseCaptureOutEvent>(e =>
            {
                Undo.CollapseUndoOperations(currentUndoGroup);
            });

            slider.RegisterValueChangedCallback(e =>
            {
                floatField.value = e.newValue;
            });

            container.Add(slider);
            _injectedElements.Add(slider);
        }

        private void UpdateIntegerField(IntegerField integerField, FieldModel fieldModel)
        {
            if (!fieldModel.UseSlider)
            {
                return;
            }

            integerField.style.width = 50;
            integerField.parent.style.flexDirection = FlexDirection.RowReverse;

            var container = integerField.parent;

            var sliderMin = (int)fieldModel.Min;
            var sliderMax = (int)fieldModel.Max;
            var slider = new SliderInt()
            {
                lowValue = sliderMin,
                highValue = sliderMax,
            };
            slider.style.flexGrow = 1;

            Undo.undoRedoPerformed += () =>
            {
                // Schedule update because value may not have actually changed yet
                EditorApplication.delayCall += () => slider.value = integerField.value;
            };

            integerField.RegisterValueChangedCallback(e =>
            {
                var value = Mathf.Clamp(e.newValue, sliderMin, sliderMax);
                integerField.value = value;
                slider.value = value;
            });

            var currentUndoGroup = 0;
            slider.RegisterCallback<MouseCaptureEvent>(e =>
            {
                Undo.SetCurrentGroupName("Update Values");
                currentUndoGroup = Undo.GetCurrentGroup();
            });

            slider.RegisterCallback<MouseCaptureOutEvent>(e =>
            {
                Undo.CollapseUndoOperations(currentUndoGroup);
            });

            slider.RegisterValueChangedCallback(e =>
            {
                integerField.value = e.newValue;
            });

            container.Add(slider);
            _injectedElements.Add(slider);
        }
    }
}
