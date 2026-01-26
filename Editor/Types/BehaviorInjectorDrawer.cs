using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(BehaviorInjector))]
    public class BehaviorInjectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as BehaviorInjector;

            // Node list preview can have a null target
            if (target != null)
            {
                root.RegisterCallback<AttachToPanelEvent>(_ => TryUpdateFields(target, root));
            }

            return root;
        }

        private void TryUpdateFields(BehaviorInjector injector, VisualElement root)
        {
            if (string.IsNullOrEmpty(injector.TypeName))
            {
                // Try again shortly
                root.schedule.Execute(() => TryUpdateFields(injector, root)).StartingIn(100);
                return;
            }

            var classModel = ClassModelCache.GetClassModel(injector.TypeName);
            UpdateFields(root, classModel);
        }

        private void UpdateFields(VisualElement root, ClassModel classModel)
        {
            if (TryFindAncestorByName(root, "node-options", out var optionsRoot))
            {
                var optionsParent = optionsRoot.parent;

                var portsRoot = optionsParent.Q("port-container");
                if (portsRoot != null)
                {
                    var inputPortsRoot = portsRoot.Q("inputs");
                    var connectors = inputPortsRoot.Query("connector-container").Build().ToList();
                    var editors = inputPortsRoot.Query("constant-editor").Build().ToList();

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
                                var fieldModel = classModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                                UpdateFloatField(floatField, fieldModel);

                                continue;
                            }

                            var integerField = editorElement.Q<IntegerField>();
                            if (integerField != null)
                            {
                                var fieldModel = classModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                                UpdateIntegerField(integerField, fieldModel);

                                continue;
                            }

                            var textField = editorElement.Q<TextField>();
                            if (textField != null)
                            {
                                var fieldModel = classModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                                UpdateTextField(textField, fieldModel);

                                continue;
                            }
                        }
                    }
                }
            }
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
        }
    }
}
