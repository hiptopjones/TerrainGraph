using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CodeFirst.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(BehaviorInjector))]
    public class BehaviorInjectorDrawer : PropertyDrawer
    {
        private int _generationCount;
        private int _retryCount;

        private List<VisualElement> _injectedElements = new();

        private SerializedObject _serializedObject;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            // Useful to debug property problems
            var debug = new VisualElement();
            debug.AddToClassList("behavior-injector");
            debug.Add(new PropertyField(property.FindPropertyRelative("InputsTypeName")));
            debug.Add(new PropertyField(property.FindPropertyRelative("OptionsTypeName")));
            debug.style.display = DisplayStyle.None;
            root.Add(debug);

            var target = fieldInfo.GetValue(property.serializedObject.targetObject) as BehaviorInjector;
            if (target != null)
            {
                _serializedObject = property.serializedObject;

                root.RegisterCallback<AttachToPanelEvent>(_ => AddInjectedBehavior(property, root, _generationCount));
                root.RegisterCallback<DetachFromPanelEvent>(_ => RemoveInjectedBehavior(property, root, _generationCount));
            }

            return root;
        }

        private void AddInjectedBehavior(SerializedProperty property, VisualElement root, int generationCount)
        {
            if (generationCount != _generationCount)
            {
                // We are out of sync, so exit early
                // Can happen if we are in a retry conditoin and a detach event came in
                return;
            }

            if (TryFindAncestorByName(root, "libraryViewContainer", out _) ||
                TryFindAncestorByName(root, "Graph Inspector", out _))
            {
                // Ignore the item library and the inspector
                // We only want to inject node UI that is in the graph view
                return;
            }

            var inputsTypeName = property.FindPropertyRelative("InputsTypeName")?.stringValue;
            var optionsTypeName = property.FindPropertyRelative("OptionsTypeName")?.stringValue;

            if (!string.IsNullOrEmpty(inputsTypeName))
            {
                _retryCount = 0;

                var optionsModel = ClassModelCache.GetClassModel(optionsTypeName);
                var inputsModel = ClassModelCache.GetClassModel(inputsTypeName);

                UpdateFields(root, inputsModel, optionsModel, generationCount);
            }
            else
            {
                if (_retryCount > 3)
                {
                    // Injector properties could not be retrieved
                    return;
                }

                // Try again shortly, when hopefully the node has been able to set the type name
                EditorApplication.delayCall += () => AddInjectedBehavior(property, root, generationCount);

                _retryCount++;
            }
        }

        private void RemoveInjectedBehavior(SerializedProperty property, VisualElement root, int generationCount)
        {
            // Helps prevent getting out of sync
            _generationCount++;

            foreach (var element in _injectedElements)
            {
                element.RemoveFromHierarchy();
            }

            _injectedElements.Clear();
        }

        private void UpdateFields(VisualElement root, ClassModel inputsModel, ClassModel optionsModel, int generationCount)
        {
            if (generationCount != _generationCount)
            {
                // We are out of sync, so exit early
                // Can happen if we are in a retry conditoin and a detach event came in
                return;
            }

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
                _retryCount = 0;

                ProcessOptions(optionsRoot, optionsModel);
                ProcessInputs(inputsRoot, inputsModel);
                AddPreviewPanel(optionsRoot.parent);
            }
            else
            {
                if (_retryCount > 3)
                {
                    // Visual tree not ready
                    return;
                }

                // Try again shortly, when hopefully everything is in place
                EditorApplication.delayCall += () => UpdateFields(root, inputsModel, optionsModel, generationCount);

                _retryCount++;
            }
        }

        private void AddPreviewPanel(VisualElement root)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center,
                }
            };

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    flexGrow = 1,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0,0,0,0.25f),
                    borderBottomColor = new Color(0,0,0,0.25f),
                    borderLeftColor = new Color(0,0,0,0.25f),
                    borderRightColor = new Color(0,0,0,0.25f),
                    marginBottom = 6,
                }
            };
            container.Add(image);

            var label = new Label();
            container.Add(label);

            _serializedObject.Update();

            var injector = fieldInfo.GetValue(_serializedObject.targetObject) as BehaviorInjector;
            if (injector != null)
            {
                image.image = injector.PreviewTexture;
                label.text = injector.PreviewDescription;

                injector.UpdatePreview += () =>
                {
                    image.image = injector.PreviewTexture;
                    label.text = injector.PreviewDescription;
                };
            }

            root.Add(container);
            _injectedElements.Add(container);
        }

        private void ProcessOptions(VisualElement optionsRoot, ClassModel optionsModel)
        {
            var isSeparatorInsertIndexSet = false;
            var separatorInsertIndex = 0;

            for (int i = 0; i < optionsRoot.childCount; i++)
            {
                var optionElement = optionsRoot[i];

                var label = optionElement.Q<Label>();
                if (label == null)
                {
                    continue;
                }

                var editorElement = label.parent;
                var displayName = label.text;

                var fieldModel = optionsModel.FieldModels.FirstOrDefault(x => x.DisplayName == displayName);
                if (fieldModel == null)
                {
                    continue;
                }

                if (!fieldModel.IsCustom)
                {
                    // Add the separator just before the first base option
                    if (!isSeparatorInsertIndexSet)
                    {
                        separatorInsertIndex = i;
                        isSeparatorInsertIndexSet = true;
                    }
                }

                var toggleField = editorElement as Toggle;
                if (toggleField != null)
                {
                    if (fieldModel.Name == "IsNodeDisabled")
                    {
                        AddDisabledBanner(toggleField, optionsRoot);
                    }

                    if (fieldModel.Name == "IsPreviewEnabled")
                    {
                        InsertPreviewButton(toggleField, fieldModel);
                    }
                }
            }

            // Delay the separator insertion until after looping, otherwise the child count changes mid-loop
            if (isSeparatorInsertIndexSet)
            {
                if (separatorInsertIndex > 0)
                {
                    // No separator if there were no custom options (index = 0)
                    InsertSeparator(optionsRoot, separatorInsertIndex);
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

        private void InsertSeparator(VisualElement optionsRoot, int index)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    borderBottomColor = new Color(0x22/256f, 0x22/256f, 0x22/256f),  // TODO: What about light mode?
                    borderBottomWidth = 2,
                    marginTop = 5,
                    marginBottom = 5
                }
            };

            optionsRoot.Insert(index, container);
            _injectedElements.Add(container);
        }

        private void InsertPreviewButton(Toggle toggle, FieldModel fieldModel)
        {
            _serializedObject.Update();

            var injector = fieldInfo.GetValue(_serializedObject.targetObject) as BehaviorInjector;
            if (injector.SetMeshPreview == null)
            {
                // Not applicable for this node
                return;
            }

            var container = toggle.parent;

            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;

            var button = new Button();
            button.text = "Preview Mesh";
            button.style.backgroundColor = new Color(0.3f, 0.1f, 0); // TODO: What about light mode
            button.clicked += () =>
            {
                _serializedObject.Update();

                var injector = fieldInfo.GetValue(_serializedObject.targetObject) as BehaviorInjector;
                if (injector != null)
                {
                    injector.SetMeshPreview();
                }
            };

            container.Add(button);
            _injectedElements.Add(button);
        }

        private void AddDisabledBanner(Toggle toggle, VisualElement optionsRoot)
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
            if (!fieldModel.UseLinearSlider && !fieldModel.UsePowerSlider)
            {
                return;
            }

            floatField.style.width = 50;
            floatField.parent.style.flexDirection = FlexDirection.RowReverse;

            var container = floatField.parent;

            var rangeMin = (float)fieldModel.Min;
            var rangeMax = (float)fieldModel.Max;

            var sliderMin = fieldModel.UseLinearSlider ? rangeMin : 0;
            var sliderMax = fieldModel.UseLinearSlider ? rangeMax : 1;

            var slider = new Slider()
            {
                lowValue = sliderMin,
                highValue = sliderMax,
            };
            slider.style.flexGrow = 1;
            slider.style.minWidth = 150;

            slider.value = fieldModel.UseLinearSlider ?
                    floatField.value :
                    PowerToLinear(floatField.value, rangeMin, rangeMax, fieldModel.PowerSliderPower);

            Undo.undoRedoPerformed += () =>
            {
                // Schedule update because value may not have actually changed yet
                EditorApplication.delayCall += () => slider.value = fieldModel.UseLinearSlider ?
                    floatField.value :
                    PowerToLinear(floatField.value, rangeMin, rangeMax, fieldModel.PowerSliderPower);
            };

            floatField.RegisterValueChangedCallback(e =>
            {
                // NOTE: Not clamping to allow manual entry
                floatField.value = e.newValue;
                slider.value = fieldModel.UseLinearSlider ?
                    e.newValue :
                    PowerToLinear(e.newValue, rangeMin, rangeMax, fieldModel.PowerSliderPower);
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
                floatField.value = fieldModel.UseLinearSlider ?
                    e.newValue :
                    LinearToPower(e.newValue, rangeMin, rangeMax, fieldModel.PowerSliderPower);
            });

            container.Add(slider);
            _injectedElements.Add(slider);
        }

        private void UpdateIntegerField(IntegerField integerField, FieldModel fieldModel)
        {
            if (!fieldModel.UseLinearSlider)
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
            slider.style.minWidth = 150;

            slider.value = integerField.value;

            Undo.undoRedoPerformed += () =>
            {
                // Schedule update because value may not have actually changed yet
                EditorApplication.delayCall += () => slider.value = integerField.value;
            };

            integerField.RegisterValueChangedCallback(e =>
            {
                // NOTE: Not clamping to allow manual entry
                integerField.value = e.newValue;
                slider.value = e.newValue;
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

        private float LinearToPower(float t, float min, float max, float power)
        {
            // t is 0–1
            return min + Mathf.Pow(t, power) * (max - min);
        }

        private float PowerToLinear(float value, float min, float max, float power)
        {
            float normalized = Mathf.InverseLerp(min, max, value);
            return Mathf.Pow(normalized, 1f / power);
        }
    }
}
